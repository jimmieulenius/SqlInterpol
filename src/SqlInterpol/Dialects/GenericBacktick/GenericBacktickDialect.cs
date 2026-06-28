using SqlInterpol.Dialects;

namespace SqlInterpol;

public sealed class GenericBacktickDialect : SqlDialectBase
{
    public override SqlDialectKind Kind => SqlDialectKind.GenericBacktick;
    public override string OpenQuote => "`";
    public override string CloseQuote => "`";
    public override string ParameterPrefix => "@"; 
}