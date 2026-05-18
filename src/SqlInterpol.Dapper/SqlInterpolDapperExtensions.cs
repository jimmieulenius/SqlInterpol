using System.Data;
using Dapper;

namespace SqlInterpol.Dapper;

public static class SqlInterpolDapperExtensions
{
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
            ToDynamicParameters(result.Parameters), 
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
            ToDynamicParameters(result.Parameters), 
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
            ToDynamicParameters(result.Parameters),
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
            ToDynamicParameters(result.Parameters),
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
            ToDynamicParameters(result.Parameters), 
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
            ToDynamicParameters(result.Parameters),
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
            ToDynamicParameters(result.Parameters),
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
            ToDynamicParameters(result.Parameters),
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
            ToDynamicParameters(result.Parameters),
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
            ToDynamicParameters(result.Parameters),
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
            ToDynamicParameters(result.Parameters),
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
            ToDynamicParameters(result.Parameters),
            transaction,
            commandTimeout,
            commandType);
    }

    private static DynamicParameters ToDynamicParameters(IEnumerable<KeyValuePair<string, object?>> parameters)
    {
        var dp = new DynamicParameters();
        foreach (var p in parameters)
        {
            dp.Add(p.Key, p.Value);
        }
        return dp;
    }
}