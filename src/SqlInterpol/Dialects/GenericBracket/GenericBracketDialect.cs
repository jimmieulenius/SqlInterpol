using SqlInterpol.Configuration;

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

    /// <inheritdoc />
    public override SqlDialectKind Kind => SqlDialectKind.GenericBracket;
    
    /// <inheritdoc />
    public override string OpenQuote => _openQuote;
    
    /// <inheritdoc />
    public override string CloseQuote => _closeQuote;
    
    /// <inheritdoc />
    public override string ParameterPrefix => "@"; 
}