using SqlInterpol.Config;
using SqlInterpol.Dialects;

namespace SqlInterpol;

public static partial class SqlBuilderExtensions
{
    private static class DialectCache<T> where T : ISqlDialect, new()
    {
        public static readonly T Instance = new();
    }

    extension (SqlBuilder _)
    {
        public static SqlBuilder PostgreSql(SqlInterpolOptions? opt = null) 
            => new(DialectCache<PostgreSqlSqlDialect>.Instance, opt);

        public static SqlBuilder MySql(SqlInterpolOptions? opt = null) 
            => new(DialectCache<MySqlSqlDialect>.Instance, opt);

        public static SqlBuilder Oracle(SqlInterpolOptions? opt = null) 
            => new(DialectCache<OracleSqlDialect>.Instance, opt);

        public static SqlBuilder SqLite(SqlInterpolOptions? opt = null) 
            => new(DialectCache<SqLiteSqlDialect>.Instance, opt);

        public static SqlBuilder SqlServer(SqlInterpolOptions? opt = null) 
            => new(DialectCache<SqlServerSqlDialect>.Instance, opt);

        public static SqlBuilder ForDialect<TDialect>(SqlInterpolOptions? opt = null) 
            where TDialect : ISqlDialect, new()
            => new(DialectCache<TDialect>.Instance, opt);
    }
}