namespace SqlInterpol.Services;

public class MySqlDialectService : SqlDialectServiceBase
{
    public override string OpenQuote => "`";
    public override string CloseQuote => "`";
    public override string ParameterPrefix => "@p";
}