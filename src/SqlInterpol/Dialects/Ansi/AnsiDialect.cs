using SqlInterpol.Dialects;

namespace SqlInterpol;

/// <summary>
/// Represents the standard ANSI SQL dialect. 
/// Used as the default engine for compiling vendor-neutral templates.
/// </summary>
public class AnsiDialect : SqlDialectBase
{
    public override SqlDialectKind Kind => SqlDialectKind.Ansi;
    public override string OpenQuote => "\"";
    public override string CloseQuote => "\"";
    public override string ParameterPrefix => "@"; 
}