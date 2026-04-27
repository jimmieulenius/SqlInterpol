namespace SqlInterpol.Services;

public class SqlServerDialectService : SqlDialectServiceBase
{
    public override string OpenQuote => "[";
    public override string CloseQuote => "]";
    public override string ParameterPrefix => "@p";
}