namespace SqlInterpol.Dialects;

/// <summary>
/// A generic dialect that uses backticks for identifiers. 
/// Useful as a fallback for Google BigQuery, Presto, or obscure MySQL-adjacent databases.
/// </summary>
[SqlDialect(OpenQuote = _openQuote, CloseQuote = _closeQuote)]
public sealed class GenericBacktickSqlDialect : SqlDialectBase
{
    private const string _openQuote = "`";
    private const string _closeQuote = "`";

    public override SqlDialectKind Kind => SqlDialectKind.GenericBacktick;
    
    public override string OpenQuote => _openQuote;
    public override string CloseQuote => _closeQuote;
    public override string ParameterPrefix => "@"; 
}