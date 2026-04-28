using SqlInterpol.Config;

namespace SqlInterpol.Dialects;

public class SqLiteSqlDialect : SqlDialectBase
{
    public override SqlDialectKind Kind => SqlDialectKind.SqLite;
    public override string OpenQuote => "\"";
    public override string CloseQuote => "\"";
    public override string ParameterPrefix => "?";
}