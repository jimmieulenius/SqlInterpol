namespace SqlInterpol.Services;

public class SqLiteDialectService : SqlDialectServiceBase
{
    public override string OpenQuote => "\"";
    public override string CloseQuote => "\"";
    public override string ParameterPrefix => "?";
}