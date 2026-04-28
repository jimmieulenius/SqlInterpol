using SqlInterpol.Config;

namespace SqlInterpol.Dialects;

public class MySqlSqlDialect : SqlDialectBase
{
    public override SqlDialectKind Kind => SqlDialectKind.MySql;
    public override string OpenQuote => "`";
    public override string CloseQuote => "`";
    public override string ParameterPrefix => "@p";
}