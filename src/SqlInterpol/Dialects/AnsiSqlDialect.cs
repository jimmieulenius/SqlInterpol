namespace SqlInterpol.Dialects;

/// <summary>
/// Represents the standard ANSI SQL dialect. 
/// Used as the default engine for compiling vendor-neutral templates, and can be used for generic database connections.
/// </summary>
public class AnsiSqlDialect : SqlDialectBase
{
    public override SqlDialectKind Kind => SqlDialectKind.Ansi;
    
    public override string OpenQuote => "\"";
    public override string CloseQuote => "\"";
    public override string ParameterPrefix => "@"; 
}