using SqlInterpol.Config;
using SqlInterpol.Dialects;

namespace SqlInterpol;

public static partial class SqlBuilderExtensions
{
    private static readonly PostgreSqlSqlDialect _postgreSql = new();
    private static readonly MySqlSqlDialect _mySql = new();
    private static readonly OracleSqlDialect _oracle = new();
    private static readonly SqLiteSqlDialect _sqLite = new();
    private static readonly SqlServerSqlDialect _sqlServer = new();

    private static class DialectCache<T> where T : ISqlDialect, new()
    {
        public static readonly T Instance = new();
    }

    extension (SqlBuilder _)
    {
        public static SqlBuilder PostgreSql(SqlInterpolOptions? opt = null) 
            => new(_postgreSql, opt);

        public static SqlBuilder MySql(SqlInterpolOptions? opt = null) 
            => new(_mySql, opt);

        public static SqlBuilder Oracle(SqlInterpolOptions? opt = null) 
            => new(_oracle, opt);

        public static SqlBuilder SqLite(SqlInterpolOptions? opt = null) 
            => new(_sqLite, opt);

        public static SqlBuilder SqlServer(SqlInterpolOptions? opt = null) 
            => new(_sqlServer, opt);

        public static SqlBuilder ForDialect<TDialect>(SqlInterpolOptions? opt = null) 
            where TDialect : ISqlDialect, new()
            => new(DialectCache<TDialect>.Instance, opt);
    }
}