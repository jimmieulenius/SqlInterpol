using SqlInterpol.Dialects;

namespace SqlInterpol;

[SqlDialect(OpenQuote = _openQuote, CloseQuote = _closeQuote)]
public sealed class GenericBracketDialect : SqlDialectBase
{
    private const string _openQuote = "[";
    private const string _closeQuote = "]";

    public override SqlDialectKind Kind => SqlDialectKind.GenericBracket;
    public override string OpenQuote => _openQuote;
    public override string CloseQuote => _closeQuote;
    public override string ParameterPrefix => "@"; 
}