using SqlInterpol.Config;
using SqlInterpol.Dialects;

namespace SqlInterpol;

public static partial class SqlBuilderExtensions
{
    extension (SqlBuilder b)
    {
        

        public static SqlBuilder PostgreSql(SqlInterpolOptions? opt = null) 
            => new(new PostgreSqlSqlDialect(), opt);

        public static SqlBuilder MySql(SqlInterpolOptions? opt = null) 
            => new(new MySqlSqlDialect(), opt);

        public static SqlBuilder Oracle(SqlInterpolOptions? opt = null) 
            => new(new OracleSqlDialect(), opt);

        public static SqlBuilder SqLite(SqlInterpolOptions? opt = null) 
            => new(new SqLiteSqlDialect(), opt);

        public static SqlBuilder SqlServer(SqlInterpolOptions? opt = null) 
            => new(new SqlServerSqlDialect(), opt);

        // Generic fallback for any custom dialect
        public static SqlBuilder ForDialect<TDialect>(SqlInterpolOptions? opt = null) 
            where TDialect : ISqlDialect, new()
            => new(new TDialect(), opt);
    }
}