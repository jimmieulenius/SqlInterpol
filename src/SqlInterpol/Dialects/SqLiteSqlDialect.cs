namespace SqlInterpol.Dialects;

public class SqLiteSqlDialect : SqlDialectBase
{
    public override string OpenQuote => "\"";
    public override string CloseQuote => "\"";
    public override string ParameterPrefix => "?";
}