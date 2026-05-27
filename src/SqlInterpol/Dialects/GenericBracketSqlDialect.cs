    namespace SqlInterpol.Dialects;

/// <summary>
/// A generic dialect that uses square brackets for identifiers. 
/// Useful as a fallback for MS Access, Sybase, and legacy Microsoft-adjacent datastores.
/// </summary>
public sealed class GenericBracketSqlDialect : SqlDialectBase
{
    public override SqlDialectKind Kind => SqlDialectKind.GenericBracket;
    
    public override string OpenQuote => "[";
    public override string CloseQuote => "]";
    
    public override string ParameterPrefix => "@"; 
}