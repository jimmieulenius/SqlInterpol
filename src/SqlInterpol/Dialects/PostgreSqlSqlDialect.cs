using SqlInterpol.Config;

namespace SqlInterpol.Dialects;

public class PostgreSqlSqlDialect : SqlDialectBase
{
    public override SqlDialectKind Kind => SqlDialectKind.PostgreSql;
    public override string OpenQuote => "\"";
    public override string CloseQuote => "\"";
    public override string ParameterPrefix => "$";
}