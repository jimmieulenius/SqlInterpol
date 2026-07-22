using SqlInterpol.Configuration;

namespace SqlInterpol.Dialects;

/// <summary>
/// Represents the standard ANSI SQL dialect. 
/// Used as the default engine for processing vendor-neutral templates, and can be used for generic database connections.
/// </summary>
[SqlDialect(OpenQuote = _openQuote, CloseQuote = _closeQuote)]
public class AnsiSqlDialect : SqlDialectBase
{
    private const string _openQuote = "\"";
    private const string _closeQuote = "\"";

    /// <inheritdoc />
    public override SqlDialectKind Kind => SqlDialectKind.Ansi;
    
    /// <inheritdoc />
    public override string OpenQuote => _openQuote;
    
    /// <inheritdoc />
    public override string CloseQuote => _closeQuote;
    
    /// <inheritdoc />
    public override string ParameterPrefix => "@"; 
}