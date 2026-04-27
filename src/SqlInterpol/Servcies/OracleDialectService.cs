namespace SqlInterpol.Services;

public class OracleDialectService : SqlDialectServiceBase
{
    public override string OpenQuote => "\"";
    public override string CloseQuote => "\"";
    public override string ParameterPrefix => ":";
}