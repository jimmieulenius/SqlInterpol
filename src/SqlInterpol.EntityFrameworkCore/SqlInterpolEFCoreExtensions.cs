using System.Data.Common;
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using SqlInterpol.Dialects;

namespace SqlInterpol.EFCore;

public static class SqlInterpolEFCoreExtensions
{
    public static SqlBuilder CreateSqlBuilder(this DbContext context, SqlInterpolOptions? options = null)
    {
        var connection = context.Database.GetDbConnection();
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
                "Please instantiate the SqlBuilder manually.")
        };

        return new SqlBuilder(dialect, options);
    }

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

    public static ModelBuilder MapEntity<T>(this ModelBuilder modelBuilder, SqlInterpolOptions? options = null) where T : class
    {
        var meta = SqlMetadataRegistry.GetMetadata<T>();
        options ??= new SqlInterpolOptions(); 
        
        modelBuilder.Entity<T>().ToTable(meta.Name);

        foreach (var kvp in meta.Columns)
        {
            // FIX: Cast the MemberInfo back to PropertyInfo
            if (kvp.Key is not PropertyInfo propertyInfo) continue;

            var columnName = kvp.Value;
            
            var propertyBuilder = modelBuilder.Entity<T>()
                .Property(propertyInfo.Name)
                .HasColumnName(columnName);

            // SYNC ENUMS WITH EF CORE!
            if (SqlMetadataRegistry.IsScalarType(propertyInfo.PropertyType))
            {
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
        }
        
        return modelBuilder;
    }
}