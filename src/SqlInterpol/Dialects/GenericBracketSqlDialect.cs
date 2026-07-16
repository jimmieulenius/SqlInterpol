    namespace SqlInterpol.Dialects;

/// <summary>
/// A generic dialect that uses square brackets for identifiers. 
/// Useful as a fallback for MS Access, Sybase, and legacy Microsoft-adjacent datastores.
/// </summary>
[SqlDialect(OpenQuote = _openQuote, CloseQuote = _closeQuote)]
public sealed class GenericBracketSqlDialect : SqlDialectBase
{
    private const string _openQuote = "[";
    private const string _closeQuote = "]";

    public override SqlDialectKind Kind => SqlDialectKind.GenericBracket;
    
    public override string OpenQuote => _openQuote;
    public override string CloseQuote => _closeQuote;
    
    public override string ParameterPrefix => "@"; 
}