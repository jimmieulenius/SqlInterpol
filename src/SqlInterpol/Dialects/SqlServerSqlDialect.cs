namespace SqlInterpol.Dialects;

public class SqlServerSqlDialect : SqlDialectBase
{
    public override string OpenQuote => "[";
    public override string CloseQuote => "]";
    public override string ParameterPrefix => "@p";
}