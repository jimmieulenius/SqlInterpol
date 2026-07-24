using System;
using System.Data.Common;
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using SqlInterpol.Configuration;
using SqlInterpol.Dialects;
using SqlInterpol.Schema;

namespace SqlInterpol.EFCore;

public static class SqlInterpolEFCoreExtensions
{
    public static SqlBuilder CreateSqlBuilder(this DbContext context, SqlInterpolOptions? options = null)
        => new(DetectDialect(context), options);

    // Prefers EF Core's ProviderName (immune to connection wrappers), then falls back
    // to walking the connection type hierarchy for non-standard or unconfigured providers.
    private static ISqlDialect DetectDialect(DbContext context)
    {
        var dialect = TryMatchProviderName(context.Database.ProviderName)
                   ?? TryMatchConnection(context.Database.GetDbConnection());

        if (dialect != null) return dialect;

        throw new NotSupportedException(
            $"The EF Core provider '{context.Database.ProviderName}' is not automatically mapped to a known SQL dialect. " +
            "Instantiate SqlBuilder manually and provide a custom ISqlDialect.");
    }

    // EF Core provider names are stable, versioned package identifiers — the most reliable signal.
    private static ISqlDialect? TryMatchProviderName(string? providerName) => providerName switch
    {
        "Microsoft.EntityFrameworkCore.SqlServer"   => new SqlServerDialect(),
        "Npgsql.EntityFrameworkCore.PostgreSQL"     => new PostgreSqlDialect(),
        "Microsoft.EntityFrameworkCore.Sqlite"      => new SqLiteDialect(),
        // Both Pomelo (community) and Oracle's official MySQL provider are supported
        "Pomelo.EntityFrameworkCore.MySql"
        or "MySql.EntityFrameworkCore"
        or "MySql.Data.EntityFrameworkCore"         => new MySqlDialect(),
        "Oracle.EntityFrameworkCore"                => new OracleDialect(),
        "FirebirdSql.EntityFrameworkCore.Firebird"  => new FirebirdDialect(),
        _ => null
    };

    // Fallback: walk the connection type hierarchy so wrappers (MiniProfiler, OpenTelemetry, etc.) are handled.
    private static ISqlDialect? TryMatchConnection(DbConnection connection)
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

    // Namespace-guarded for SqlConnection: both Microsoft.Data.SqlClient and System.Data.SqlClient use that name.
    private static ISqlDialect? TryMatchConnectionType(Type type) => type.Name switch
    {
        "SqlConnection" when type.Namespace is "Microsoft.Data.SqlClient" or "System.Data.SqlClient"
            => new SqlServerDialect(),
        "NpgsqlConnection"  => new PostgreSqlDialect(),
        "SqliteConnection"  => new SqLiteDialect(),
        "MySqlConnection"   => new MySqlDialect(),
        "OracleConnection"  => new OracleDialect(),
        "FbConnection"      => new FirebirdDialect(),
        _ => null
    };

    public static DbParameter[] ToDbParameters(this SqlQueryResult result, DbContext context)
    {
        // Using the connection to spawn parameters ensures we get the correct type 
        // (e.g., SqliteParameter vs NpgsqlParameter) without hardcoded provider dependencies!
        using var cmd = context.Database.GetDbConnection().CreateCommand();
        
        var parameters = new DbParameter[result.Parameters.Count];
        int i = 0;
        
        foreach (var kvp in result.Parameters)
        {
            var p = cmd.CreateParameter();
            p.ParameterName = kvp.Key;
            p.Value = kvp.Value ?? DBNull.Value;
            parameters[i++] = p;
        }
        
        return parameters;
    }

    public static IQueryable<TEntity> FromSql<TEntity>(this DbContext context, SqlQueryResult result) where TEntity : class
    {
        return context.Set<TEntity>().FromSqlRaw(result.Sql, result.ToDbParameters(context));
    }

    public static int ExecuteSql(this DbContext context, SqlQueryResult result)
    {
        return context.Database.ExecuteSqlRaw(result.Sql, result.ToDbParameters(context));
    }

    public static Task<int> ExecuteSqlAsync(this DbContext context, SqlQueryResult result, CancellationToken cancellationToken = default)
    {
        return context.Database.ExecuteSqlRawAsync(result.Sql, result.ToDbParameters(context), cancellationToken);
    }

    public static ModelBuilder MapSqlEntity<T>(this ModelBuilder modelBuilder, SqlInterpolOptions? options = null) where T : class
    {
        var meta = SqlMetadataRegistry.GetMetadata<T>();
        options ??= SqlInterpolOptions.DefaultFactory?.Invoke() ?? new SqlInterpolOptions();

        // Respect [SqlView] vs [SqlTable] — views must not be treated as tables by EF Core migrations.
        if (meta.Type == SqlEntityType.View)
            modelBuilder.Entity<T>().ToView(meta.Name, meta.Schema);
        else
            modelBuilder.Entity<T>().ToTable(meta.Name, meta.Schema);

        foreach (var kvp in meta.Columns)
        {
            if (kvp.Key is not PropertyInfo propertyInfo) continue;

            var columnName = kvp.Value;
            
            var propertyBuilder = modelBuilder.Entity<T>()
                .Property(propertyInfo.Name)
                .HasColumnName(columnName);

            var underlyingType = Nullable.GetUnderlyingType(propertyInfo.PropertyType) ?? propertyInfo.PropertyType;
            
            if (underlyingType.IsEnum)
            {
                var enumAttr = propertyInfo.GetCustomAttribute<SqlEnumFormatAttribute>();
                var format = enumAttr?.Format ?? options.EnumFormat;

                if (format == SqlEnumFormat.String)
                {
                    propertyBuilder.HasConversion<string>();
                }
            }
        }
        
        return modelBuilder;
    }
}