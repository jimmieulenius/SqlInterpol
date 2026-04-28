namespace SqlInterpol.Dialects;

public class OracleSqlDialect : SqlDialectBase
{
    public override string OpenQuote => "\"";
    public override string CloseQuote => "\"";
    public override string ParameterPrefix => ":";
}