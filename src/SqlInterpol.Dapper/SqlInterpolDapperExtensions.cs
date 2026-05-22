using System.Data;
using Dapper;
using SqlInterpol.Dialects;

namespace SqlInterpol.Dapper;

public static class SqlInterpolDapperExtensions
{
    public static SqlBuilder CreateSqlBuilder(this IDbConnection connection, SqlInterpolOptions? options = null)
    {
        string typeName = connection.GetType().Name;

        ISqlDialect dialect = typeName switch
        {
            "SqlConnection" => new SqlServerSqlDialect(),
            "NpgsqlConnection" => new PostgreSqlSqlDialect(),
            "SqliteConnection" => new SqLiteSqlDialect(),
            "MySqlConnection" => new MySqlSqlDialect(),
            "OracleConnection" => new OracleSqlDialect(),
            "FbConnection" => new FirebirdSqlDialect(),
            _ => throw new NotSupportedException(
                $"The connection type '{typeName}' is not automatically mapped to a known SQL Dialect. " +
                "Please instantiate the SqlBuilder manually and provide a custom ISqlDialect.")
        };

        return new SqlBuilder(dialect, options);
    }

    // Extends IDbConnection to match native Dapper syntax perfectly
    public static Task<IEnumerable<T>> QueryAsync<T>(
        this IDbConnection connection,
        SqlQueryResult result, 
        IDbTransaction? transaction = null,
        int? commandTimeout = null,
        CommandType? commandType = null)
    {
        return connection.QueryAsync<T>(
            result.Sql, 
            ToDynamicParameters(result), 
            transaction, 
            commandTimeout, 
            commandType);
    }

    public static Task<T?> QueryFirstOrDefaultAsync<T>(
        this IDbConnection connection,
        SqlQueryResult result, 
        IDbTransaction? transaction = null,
        int? commandTimeout = null,
        CommandType? commandType = null)
    {
        return connection.QueryFirstOrDefaultAsync<T>(
            result.Sql, 
            ToDynamicParameters(result), 
            transaction, 
            commandTimeout, 
            commandType);
    }

    public static Task<T> QuerySingleAsync<T>(
        this IDbConnection connection,
        SqlQueryResult result,
        IDbTransaction? transaction = null,
        int? commandTimeout = null,
        CommandType? commandType = null)
    {
        return connection.QuerySingleAsync<T>(
            result.Sql,
            ToDynamicParameters(result),
            transaction,
            commandTimeout,
            commandType);
    }

    public static Task<T?> QuerySingleOrDefaultAsync<T>(
        this IDbConnection connection,
        SqlQueryResult result,
        IDbTransaction? transaction = null,
        int? commandTimeout = null,
        CommandType? commandType = null)
    {
        return connection.QuerySingleOrDefaultAsync<T>(
            result.Sql,
            ToDynamicParameters(result),
            transaction,
            commandTimeout,
            commandType);
    }

    public static Task<int> ExecuteAsync(
        this IDbConnection connection,
        SqlQueryResult result, 
        IDbTransaction? transaction = null,
        int? commandTimeout = null,
        CommandType? commandType = null)
    {
        return connection.ExecuteAsync(
            result.Sql, 
            ToDynamicParameters(result), 
            transaction, 
            commandTimeout, 
            commandType);
    }

    public static Task<T?> ExecuteScalarAsync<T>(
        this IDbConnection connection,
        SqlQueryResult result,
        IDbTransaction? transaction = null,
        int? commandTimeout = null,
        CommandType? commandType = null)
    {
        return connection.ExecuteScalarAsync<T?>(
            result.Sql,
            ToDynamicParameters(result),
            transaction,
            commandTimeout,
            commandType);
    }
    public static SqlMapper.GridReader QueryMultiple(
        this IDbConnection connection,
        SqlQueryResult result,
        IDbTransaction? transaction = null,
        int? commandTimeout = null,
        CommandType? commandType = null)
    {
        return connection.QueryMultiple(
            result.Sql,
            ToDynamicParameters(result),
            transaction,
            commandTimeout,
            commandType);
    }

    public static Task<SqlMapper.GridReader> QueryMultipleAsync(
        this IDbConnection connection,
        SqlQueryResult result,
        IDbTransaction? transaction = null,
        int? commandTimeout = null,
        CommandType? commandType = null)
    {
        return connection.QueryMultipleAsync(
            result.Sql,
            ToDynamicParameters(result),
            transaction,
            commandTimeout,
            commandType);
    }
    public static IEnumerable<T> Query<T>(
        this IDbConnection connection,
        SqlQueryResult result,
        IDbTransaction? transaction = null,
        bool buffered = true,
        int? commandTimeout = null,
        CommandType? commandType = null)
    {
        return connection.Query<T>(
            result.Sql,
            ToDynamicParameters(result),
            transaction,
            buffered,
            commandTimeout,
            commandType);
    }

    public static T? QueryFirstOrDefault<T>(
        this IDbConnection connection,
        SqlQueryResult result,
        IDbTransaction? transaction = null,
        int? commandTimeout = null,
        CommandType? commandType = null)
    {
        return connection.QueryFirstOrDefault<T>(
            result.Sql,
            ToDynamicParameters(result),
            transaction,
            commandTimeout,
            commandType);
    }

    public static T QuerySingle<T>(
        this IDbConnection connection,
        SqlQueryResult result,
        IDbTransaction? transaction = null,
        int? commandTimeout = null,
        CommandType? commandType = null)
    {
        return connection.QuerySingle<T>(
            result.Sql,
            ToDynamicParameters(result),
            transaction,
            commandTimeout,
            commandType);
    }

    public static T? QuerySingleOrDefault<T>(
        this IDbConnection connection,
        SqlQueryResult result,
        IDbTransaction? transaction = null,
        int? commandTimeout = null,
        CommandType? commandType = null)
    {
        return connection.QuerySingleOrDefault<T>(
            result.Sql,
            ToDynamicParameters(result),
            transaction,
            commandTimeout,
            commandType);
    }

    public static int Execute(
        this IDbConnection connection,
        SqlQueryResult result,
        IDbTransaction? transaction = null,
        int? commandTimeout = null,
        CommandType? commandType = null)
    {
        return connection.Execute(
            result.Sql,
            ToDynamicParameters(result),
            transaction,
            commandTimeout,
            commandType);
    }

    public static T? ExecuteScalar<T>(
        this IDbConnection connection,
        SqlQueryResult result,
        IDbTransaction? transaction = null,
        int? commandTimeout = null,
        CommandType? commandType = null)
    {
        return connection.ExecuteScalar<T?>(
            result.Sql,
            ToDynamicParameters(result),
            transaction,
            commandTimeout,
            commandType);
    }

    public static DynamicParameters ToDynamicParameters(this SqlQueryResult result)
    {
        var dapperParams = new DynamicParameters();

        foreach (var kvp in result.Parameters)
        {
            dapperParams.Add(kvp.Key, kvp.Value);
        }
        return dapperParams;
    }
}