namespace SqlInterpol.Dialects;

public class PostgreSqlSqlDialect : SqlDialectBase
{
    public override string OpenQuote => "\"";
    public override string CloseQuote => "\"";
    public override string ParameterPrefix => "$";
}