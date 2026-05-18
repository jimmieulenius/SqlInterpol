using SqlInterpol.Config;
using SqlInterpol.Dialects;

namespace SqlInterpol;

public static class SqlDialectCache<T> where T : ISqlDialect, new()
{
    public static readonly T Instance = new();
}

public partial class SqlBuilder
{
    public static SqlBuilder PostgreSql(SqlInterpolOptions? opt = null) 
        => new(SqlDialectCache<PostgreSqlSqlDialect>.Instance, opt);

    public static SqlBuilder MySql(SqlInterpolOptions? opt = null) 
        => new(SqlDialectCache<MySqlSqlDialect>.Instance, opt);

    public static SqlBuilder Oracle(SqlInterpolOptions? opt = null) 
        => new(SqlDialectCache<OracleSqlDialect>.Instance, opt);

    public static SqlBuilder SqLite(SqlInterpolOptions? opt = null) 
        => new(SqlDialectCache<SqLiteSqlDialect>.Instance, opt);

    public static SqlBuilder SqlServer(SqlInterpolOptions? opt = null) 
        => new(SqlDialectCache<SqlServerSqlDialect>.Instance, opt);

    public static SqlBuilder Dialect<TDialect>(SqlInterpolOptions? opt = null) 
        where TDialect : ISqlDialect, new()
        => new(SqlDialectCache<TDialect>.Instance, opt);
}