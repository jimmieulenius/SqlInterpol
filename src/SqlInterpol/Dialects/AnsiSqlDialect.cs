namespace SqlInterpol.Dialects;

/// <summary>
/// Represents the standard ANSI SQL dialect. 
/// Used as the default engine for compiling vendor-neutral templates, and can be used for generic database connections.
/// </summary>
[SqlDialect(OpenQuote = _openQuote, CloseQuote = _closeQuote)]
public class AnsiSqlDialect : SqlDialectBase
{
    private const string _openQuote = "\"";
    private const string _closeQuote = "\"";

    public override SqlDialectKind Kind => SqlDialectKind.Ansi;
    
    public override string OpenQuote => _openQuote;
    public override string CloseQuote => _closeQuote;
    public override string ParameterPrefix => "@"; 
}