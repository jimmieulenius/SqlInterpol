using SqlInterpol.Configuration;

namespace SqlInterpol.Dialects;

/// <summary>
/// Represents the standard ANSI SQL dialect. 
/// Used as the default engine for processing and rendering vendor-neutral templates.
/// </summary>
[SqlDialect(OpenQuote = _openQuote, CloseQuote = _closeQuote)]
public class AnsiDialect : SqlDialectBase
{
    private const string _openQuote = "\"";
    private const string _closeQuote = "\"";

    public override SqlDialectKind Kind => SqlDialectKind.Ansi;
    public override string OpenQuote => _openQuote;
    public override string CloseQuote => _closeQuote;
    public override string ParameterPrefix => "@"; 
}