namespace SqlInterpol.Services;

public class PostgreSqlDialectService : SqlDialectServiceBase
{
    public override string OpenQuote => "\"";
    public override string CloseQuote => "\"";
    public override string ParameterPrefix => "$";
}