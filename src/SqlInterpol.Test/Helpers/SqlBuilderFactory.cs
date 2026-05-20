using SqlInterpol.Test.Dialects;

namespace SqlInterpol.Test.Helpers;

public static class SqlBuilderFactory
{
    public static SqlBuilder Create(SqlDialectKind dialect, SqlInterpolOptions? options = null)
    {
        if (dialect == SqlDialectKind.CustomDb)
            return SqlBuilder.CustomDb(options);
        else if (dialect == SqlDialectKind.Firebird)
            return SqlBuilder.Firebird(options);
        else if (dialect == SqlDialectKind.MySql)
            return SqlBuilder.MySql(options);
        else if (dialect == SqlDialectKind.Oracle)
            return SqlBuilder.Oracle(options);
        else if (dialect == SqlDialectKind.PostgreSql)
            return SqlBuilder.PostgreSql(options);
        else if (dialect == SqlDialectKind.SqLite)
            return SqlBuilder.SqLite(options);
        else if (dialect == SqlDialectKind.SqlServer)
            return SqlBuilder.SqlServer(options);
        
        throw new NotSupportedException($"Unsupported SQL dialect: {dialect}");
    }
}