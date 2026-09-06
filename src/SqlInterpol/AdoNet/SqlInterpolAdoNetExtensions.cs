using System.Data;
using System.Data.Common;
using SqlInterpol.Configuration;
using SqlInterpol.Dialects;

namespace SqlInterpol.AdoNet;

public static class SqlInterpolAdoNetExtensions
{
    public static IList<Func<Type, ISqlDialect?>> ConnectionResolvers { get; } = new List<Func<Type, ISqlDialect?>>();

    public static SqlBuilder CreateSqlBuilder(this IDbConnection connection, SqlInterpolOptions? options = null)
        => new(DetectDialect(connection), options);

    public static void BindParameters(this DbCommand command, SqlQueryResult result)
    {
        foreach (var kvp in result.Parameters)
        {
            var parameter = command.CreateParameter();
            parameter.ParameterName = kvp.Key;
            parameter.Value = kvp.Value ?? DBNull.Value;
            command.Parameters.Add(parameter);
        }
    }

    // ── DbConnection Direct Execution Overloads ────────────────────────────────

    public static int ExecuteNonQuery(this DbConnection connection, SqlQueryResult result, DbTransaction? transaction = null)
    {
        using var command = connection.CreateBoundCommand(result, transaction);
        return command.ExecuteNonQuery();
    }

    public static async Task<int> ExecuteNonQueryAsync(
        this DbConnection connection, 
        SqlQueryResult result, 
        DbTransaction? transaction = null, 
        CancellationToken cancellationToken = default)
    {
        using var command = connection.CreateBoundCommand(result, transaction);
        return await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    public static object? ExecuteScalar(this DbConnection connection, SqlQueryResult result, DbTransaction? transaction = null)
    {
        using var command = connection.CreateBoundCommand(result, transaction);
        return command.ExecuteScalar();
    }

    public static async Task<object?> ExecuteScalarAsync(
        this DbConnection connection, 
        SqlQueryResult result, 
        DbTransaction? transaction = null, 
        CancellationToken cancellationToken = default)
    {
        using var command = connection.CreateBoundCommand(result, transaction);
        return await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
    }

    public static DbDataReader ExecuteReader(
        this DbConnection connection, 
        SqlQueryResult result, 
        CommandBehavior behavior = CommandBehavior.Default, 
        DbTransaction? transaction = null)
    {
        // Readers require the command to stay alive, so we do not use 'using' here.
        var command = connection.CreateBoundCommand(result, transaction);
        return command.ExecuteReader(behavior);
    }

    public static async Task<DbDataReader> ExecuteReaderAsync(
        this DbConnection connection, 
        SqlQueryResult result, 
        CommandBehavior behavior = CommandBehavior.Default, 
        DbTransaction? transaction = null, 
        CancellationToken cancellationToken = default)
    {
        var command = connection.CreateBoundCommand(result, transaction);
        return await command.ExecuteReaderAsync(behavior, cancellationToken).ConfigureAwait(false);
    }

    // ── DbCommand Execution Overloads ──────────────────────────────────────────

    public static int ExecuteNonQuery(this DbCommand command, SqlQueryResult result)
    {
        command.ApplyResult(result);
        return command.ExecuteNonQuery();
    }

    public static Task<int> ExecuteNonQueryAsync(this DbCommand command, SqlQueryResult result, CancellationToken cancellationToken = default)
    {
        command.ApplyResult(result);
        return command.ExecuteNonQueryAsync(cancellationToken);
    }

    public static object? ExecuteScalar(this DbCommand command, SqlQueryResult result)
    {
        command.ApplyResult(result);
        return command.ExecuteScalar();
    }

    public static Task<object?> ExecuteScalarAsync(this DbCommand command, SqlQueryResult result, CancellationToken cancellationToken = default)
    {
        command.ApplyResult(result);
        return command.ExecuteScalarAsync(cancellationToken);
    }

    // ── Private Helpers ────────────────────────────────────────────────────

    private static DbCommand CreateBoundCommand(this DbConnection connection, SqlQueryResult result, DbTransaction? transaction)
    {
        var command = connection.CreateCommand();
        if (transaction != null) command.Transaction = transaction;
        command.ApplyResult(result);
        return command;
    }

    private static void ApplyResult(this DbCommand command, SqlQueryResult result)
    {
        command.CommandText = result.Sql;
        command.BindParameters(result);
    }

    // ── Dialect Detection Private Helpers ──────────────────────────────────────

    private static ISqlDialect DetectDialect(IDbConnection connection)
    {
        var type = connection.GetType();
        var currentType = type;

        while (currentType != null && currentType != typeof(object))
        {
            foreach (var resolver in ConnectionResolvers)
            {
                if (resolver(currentType) is ISqlDialect customDialect) return customDialect;
            }
            currentType = currentType.BaseType;
        }

        var dialect = TryMatchConnectionHierarchy(connection);

        return dialect ?? throw new NotSupportedException(
            $"The connection type '{type.Name}' is not automatically mapped to a known SQL dialect. " +
            "Instantiate SqlBuilder manually and provide a custom ISqlDialect, or register a resolver via SqlInterpolAdoNetExtensions.ConnectionResolvers.");
    }

    private static ISqlDialect? TryMatchConnectionHierarchy(IDbConnection connection)
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