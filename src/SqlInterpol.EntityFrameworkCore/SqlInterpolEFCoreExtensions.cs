using System.Data.Common;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using SqlInterpol.Configuration;
using SqlInterpol.Dialects;
using SqlInterpol.Schema;

namespace SqlInterpol.EFCore;

/// <summary>
/// Extension methods that bridge <see cref="SqlBuilder"/> with Entity Framework Core's
/// <see cref="DbContext"/>, enabling dialect auto-detection, parameter materialization,
/// and entity-to-table mapping without hardcoding provider-specific types.
/// </summary>
public static class SqlInterpolEFCoreExtensions
{
    /// <summary>
    /// Creates a <see cref="SqlBuilder"/> whose dialect is automatically resolved from
    /// the EF Core provider configured on <paramref name="context"/>.
    /// </summary>
    /// <param name="context">The active <see cref="DbContext"/> whose provider is inspected.</param>
    /// <param name="options">Optional interpolation configuration; uses defaults when <see langword="null"/>.</param>
    /// <returns>A <see cref="SqlBuilder"/> pre-configured with the detected SQL dialect.</returns>
    /// <exception cref="NotSupportedException">
    /// Thrown when the provider cannot be mapped to a known dialect.
    /// Instantiate <see cref="SqlBuilder"/> manually with a custom <see cref="ISqlDialect"/> in that case.
    /// </exception>
    public static SqlBuilder CreateSqlBuilder(this DbContext context, SqlInterpolOptions? options = null)
        => new(DetectDialect(context), options);

    /// <summary>
    /// Materializes all parameters from a <see cref="SqlQueryResult"/> into an array of
    /// provider-native <see cref="DbParameter"/> instances by delegating creation to the
    /// connection's own <see cref="DbCommand.CreateParameter"/> factory. This guarantees
    /// the correct concrete type (e.g. <c>NpgsqlParameter</c>) without hardcoding any
    /// provider dependency.
    /// </summary>
    /// <param name="result">The query result whose <see cref="SqlQueryResult.Parameters"/> are materialized.</param>
    /// <param name="context">The active <see cref="DbContext"/> whose connection factory is used.</param>
    /// <returns>An array of <see cref="DbParameter"/> instances ready for command execution.</returns>
    public static DbParameter[] ToDbParameters(this SqlQueryResult result, DbContext context)
    {
        // Spawning parameters from the live connection guarantees the correct concrete type
        // (e.g. SqliteParameter vs NpgsqlParameter) without hardcoding any provider dependency.
        using var command = context.Database.GetDbConnection().CreateCommand();

        var parameters = new DbParameter[result.Parameters.Count];
        int index = 0;

        foreach (var kvp in result.Parameters)
        {
            var parameter = command.CreateParameter();
            parameter.ParameterName = kvp.Key;
            parameter.Value = kvp.Value ?? DBNull.Value;
            parameters[index++] = parameter;
        }

        return parameters;
    }

    /// <summary>
    /// Returns an <see cref="IQueryable{TEntity}"/> by forwarding <paramref name="result"/>
    /// to <c>FromSqlRaw</c> with all parameters materialized into provider-native
    /// <see cref="DbParameter"/> instances.
    /// </summary>
    /// <typeparam name="TEntity">The EF Core entity type to query.</typeparam>
    /// <param name="context">The active <see cref="DbContext"/>.</param>
    /// <param name="result">The SQL query and its bound parameters.</param>
    /// <returns>A composable <see cref="IQueryable{TEntity}"/>.</returns>
    public static IQueryable<TEntity> FromSql<TEntity>(this DbContext context, SqlQueryResult result)
        where TEntity : class
        => context.Set<TEntity>().FromSqlRaw(result.Sql, result.ToDbParameters(context));

    /// <summary>
    /// Executes a non-query SQL statement and returns the number of rows affected.
    /// </summary>
    /// <param name="context">The active <see cref="DbContext"/>.</param>
    /// <param name="result">The SQL statement and its bound parameters.</param>
    /// <returns>The number of state entries written to the database.</returns>
    public static int ExecuteSql(this DbContext context, SqlQueryResult result)
        => context.Database.ExecuteSqlRaw(result.Sql, result.ToDbParameters(context));

    /// <summary>
    /// Asynchronously executes a non-query SQL statement and returns the number of rows affected.
    /// </summary>
    /// <param name="context">The active <see cref="DbContext"/>.</param>
    /// <param name="result">The SQL statement and its bound parameters.</param>
    /// <param name="cancellationToken">A token to observe while waiting for the task to complete.</param>
    /// <returns>
    /// A <see cref="Task{TResult}"/> representing the asynchronous operation,
    /// containing the number of rows affected.
    /// </returns>
    public static Task<int> ExecuteSqlAsync(
        this DbContext context,
        SqlQueryResult result,
        CancellationToken cancellationToken = default)
        => context.Database.ExecuteSqlRawAsync(result.Sql, result.ToDbParameters(context), cancellationToken);

    /// <summary>
    /// Configures an EF Core entity mapping from <see cref="SqlMetadataRegistry"/> metadata,
    /// including table/view routing, column name overrides, and enum format conversions.
    /// </summary>
    /// <remarks>
    /// Column metadata is resolved via <see cref="SqlMetadataRegistry.GetMetadata{T}"/>, which
    /// uses reflection internally. This method is safe for JIT but is not compatible with
    /// Native AOT or full IL trimming, as both EF Core model building and the metadata registry
    /// rely on runtime reflection.
    /// </remarks>
    /// <typeparam name="T">The entity class to configure.</typeparam>
    /// <param name="modelBuilder">The EF Core <see cref="ModelBuilder"/> to configure.</param>
    /// <param name="options">
    /// Optional interpolation options that control global defaults such as enum formatting.
    /// Uses <see cref="SqlInterpolOptions.DefaultFactory"/> when <see langword="null"/>.
    /// </param>
    /// <returns>The same <see cref="ModelBuilder"/> for fluent chaining.</returns>
    [RequiresDynamicCode("EF Core model building uses reflection internally. This method is not compatible with Native AOT.")]
    [RequiresUnreferencedCode("Column metadata is resolved via SqlMetadataRegistry which accesses member metadata that may be trimmed in Native AOT.")]
    public static ModelBuilder MapSqlEntity<T>(this ModelBuilder modelBuilder, SqlInterpolOptions? options = null)
        where T : class
    {
        var metadata = SqlMetadataRegistry.GetMetadata<T>();
        options ??= SqlInterpolOptions.DefaultFactory?.Invoke() ?? new SqlInterpolOptions();

        // Respect [SqlView] vs [SqlTable] — views must not be treated as tables by EF Core migrations.
        if (metadata.Type == SqlEntityType.View)
            modelBuilder.Entity<T>().ToView(metadata.Name, metadata.Schema);
        else
            modelBuilder.Entity<T>().ToTable(metadata.Name, metadata.Schema);

        foreach (var (propertyInfo, columnName) in metadata.Columns)
        {
            var propertyBuilder = modelBuilder.Entity<T>()
                .Property(propertyInfo.Name)
                .HasColumnName(columnName);

            var underlyingType = Nullable.GetUnderlyingType(propertyInfo.PropertyType) ?? propertyInfo.PropertyType;

            if (underlyingType.IsEnum)
            {
                var enumAttribute = propertyInfo.GetCustomAttribute<SqlEnumFormatAttribute>();
                var format = enumAttribute?.Format ?? options.EnumFormat;

                if (format == SqlEnumFormat.String)
                    propertyBuilder.HasConversion<string>();
            }
        }

        return modelBuilder;
    }

    // ── Private helpers ────────────────────────────────────────────────────────

    // Prefers EF Core's stable ProviderName (immune to connection-wrapping middleware),
    // then falls back to walking the connection's type hierarchy for non-standard providers.
    private static ISqlDialect DetectDialect(DbContext context)
    {
        var dialect = TryMatchProviderName(context.Database.ProviderName)
                   ?? TryMatchConnectionHierarchy(context.Database.GetDbConnection());

        return dialect ?? throw new NotSupportedException(
            $"The EF Core provider '{context.Database.ProviderName}' is not automatically mapped to a known SQL dialect. " +
            "Instantiate SqlBuilder manually and provide a custom ISqlDialect.");
    }

    // EF Core provider names are stable, versioned package identifiers — the most reliable signal.
    private static ISqlDialect? TryMatchProviderName(string? providerName) => providerName switch
    {
        "Microsoft.EntityFrameworkCore.SqlServer"   => new SqlServerDialect(),
        "Npgsql.EntityFrameworkCore.PostgreSQL"     => new PostgreSqlDialect(),
        "Microsoft.EntityFrameworkCore.Sqlite"      => new SqLiteDialect(),
        // Both Pomelo (community) and Oracle's official MySQL provider are supported.
        "Pomelo.EntityFrameworkCore.MySql"
        or "MySql.EntityFrameworkCore"
        or "MySql.Data.EntityFrameworkCore"         => new MySqlDialect(),
        "Oracle.EntityFrameworkCore"                => new OracleDialect(),
        "FirebirdSql.EntityFrameworkCore.Firebird"  => new FirebirdDialect(),
        _                                           => null
    };

    // Fallback: walk the connection type hierarchy so connection-wrapping middleware
    // (MiniProfiler, OpenTelemetry, etc.) is transparently handled.
    // This path uses Type.GetType() / BaseType traversal and is not Native AOT-safe.
    [RequiresUnreferencedCode("Walking DbConnection.GetType().BaseType requires type metadata that may be trimmed in Native AOT.")]
    private static ISqlDialect? TryMatchConnectionHierarchy(DbConnection connection)
    {
        var type = connection.GetType();
        while (type != null && type != typeof(object))
        {
            var dialect = TryMatchConnectionType(type);
            if (dialect != null) return dialect;
            type = type.BaseType;
        }
        return null;
    }

    // Namespace-guarded for SqlConnection: both Microsoft.Data.SqlClient and
    // System.Data.SqlClient expose a class with the same short name.
    private static ISqlDialect? TryMatchConnectionType(Type type) => type.Name switch
    {
        "SqlConnection" when type.Namespace is "Microsoft.Data.SqlClient" or "System.Data.SqlClient"
            => new SqlServerDialect(),
        "NpgsqlConnection"  => new PostgreSqlDialect(),
        "SqliteConnection"  => new SqLiteDialect(),
        "MySqlConnection"   => new MySqlDialect(),
        "OracleConnection"  => new OracleDialect(),
        "FbConnection"      => new FirebirdDialect(),
        _                   => null
    };
}