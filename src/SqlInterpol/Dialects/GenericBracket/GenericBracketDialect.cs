using SqlInterpol.Configuration;

namespace SqlInterpol.Dialects;

/// <summary>
/// A generic SQL dialect that uses square brackets for identifier quoting.
/// Used as a fallback or baseline for SQL Server-like environments.
/// </summary>
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