using SqlInterpol.Config;

namespace SqlInterpol.Dialects;

public class SqlServerSqlDialect : SqlDialectBase
{
    public override SqlDialectKind Kind => SqlDialectKind.SqlServer;
    public override string OpenQuote => "[";
    public override string CloseQuote => "]";
    public override string ParameterPrefix => "@p";

    public override string RenderFragment(ISqlFragment fragment, ISqlContext context)
    {
        if (fragment is SqlPagingFragment p)
        {
            return $"OFFSET {p.Offset} ROWS FETCH NEXT {p.Limit} ROWS ONLY";
        }

        return base.RenderFragment(fragment, context);
    }
}