using System;
using Microsoft.EntityFrameworkCore;
using SqlInterpol.Config;

namespace SqlInterpol.EntityFrameworkCore;

public static class SqlBuilderEfCoreExtensions
{
    public static SqlBuilder CreateSqlBuilder(this DbContext dbContext, SqlInterpolOptions? options = null)
    {
        string? providerName = dbContext.Database.ProviderName;

        return providerName switch
        {
            "Microsoft.EntityFrameworkCore.SqlServer" => SqlBuilder.SqlServer(options),
            "Npgsql.EntityFrameworkCore.PostgreSQL" => SqlBuilder.PostgreSql(options),
            "Pomelo.EntityFrameworkCore.MySql" => SqlBuilder.MySql(options),
            "MySql.EntityFrameworkCore" => SqlBuilder.MySql(options), 
            "Microsoft.EntityFrameworkCore.Sqlite" => SqlBuilder.SqLite(options),
            "Oracle.EntityFrameworkCore" => SqlBuilder.Oracle(options),
            
            _ => throw new NotSupportedException(
                $"The EF Core provider '{providerName}' does not have a natively mapped SqlInterpol dialect. " +
                $"You can manually instantiate a SqlBuilder using SqlBuilder.Dialect<T>() instead.")
        };
    }
}