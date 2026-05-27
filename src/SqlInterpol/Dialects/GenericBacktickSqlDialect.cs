namespace SqlInterpol.Dialects;

/// <summary>
/// A generic dialect that uses backticks for identifiers. 
/// Useful as a fallback for Google BigQuery, Presto, or obscure MySQL-adjacent databases.
/// </summary>
public sealed class GenericBacktickSqlDialect : SqlDialectBase
{
    public override SqlDialectKind Kind => SqlDialectKind.GenericBacktick;
    
    public override string OpenQuote => "`";
    public override string CloseQuote => "`";
    public override string ParameterPrefix => "@"; 
}