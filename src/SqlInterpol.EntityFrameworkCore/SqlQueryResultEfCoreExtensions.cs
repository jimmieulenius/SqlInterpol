using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace SqlInterpol.EntityFrameworkCore;

public static class SqlQueryResultEfCoreExtensions
{
    public static IQueryable<T> FromSql<T>(this DbSet<T> dbSet, SqlQueryResult result) where T : class
    {
        var connection = dbSet.GetService<ICurrentDbContext>().Context.Database.GetDbConnection();
        var dbParams = CreateDbParameters(connection, result.Parameters);
        
#pragma warning disable EF1002
        return dbSet.FromSqlRaw(result.Sql, dbParams);
#pragma warning restore EF1002
    }

    public static Task<int> ExecuteSqlAsync(
        this DatabaseFacade database, 
        SqlQueryResult result, 
        CancellationToken cancellationToken = default)
    {
        var connection = database.GetDbConnection();
        var dbParams = CreateDbParameters(connection, result.Parameters);
        
#pragma warning disable EF1002
        return database.ExecuteSqlRawAsync(result.Sql, dbParams, cancellationToken);
#pragma warning restore EF1002
    }

    private static object[] CreateDbParameters(DbConnection connection, IEnumerable<KeyValuePair<string, object?>> parameters)
    {
        using var cmd = connection.CreateCommand();
        var paramList = new List<object>();

        foreach (var p in parameters)
        {
            var dbParam = cmd.CreateParameter();
            dbParam.ParameterName = p.Key;
            dbParam.Value = p.Value ?? DBNull.Value;
            paramList.Add(dbParam);
        }

        return paramList.ToArray();
    }
}