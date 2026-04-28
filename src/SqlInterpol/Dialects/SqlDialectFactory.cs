using SqlInterpol.Config;

namespace SqlInterpol.Dialects;

internal static class SqlDialectFactory
{
    public static ISqlDialect Create(SqlDialectKind kind) => kind switch
    {
        SqlDialectKind.MySql => new MySqlSqlDialect(),
        SqlDialectKind.Oracle => new OracleSqlDialect(),
        SqlDialectKind.PostgreSql => new PostgreSqlSqlDialect(),
        SqlDialectKind.SqLite => new SqLiteSqlDialect(),
        SqlDialectKind.SqlServer => new SqlServerSqlDialect(),
        _ => throw new NotSupportedException($"Dialect {kind} is not supported.")
    };
}