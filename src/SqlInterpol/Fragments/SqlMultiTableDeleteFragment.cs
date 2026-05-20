
namespace SqlInterpol;

public class SqlMultiTableDeleteFragment : ISqlFragment
{
    public ISqlFragment Target { get; }
    public ISqlFragment FromClause { get; }
    public ISqlFragment? WhereClause { get; }

    public SqlMultiTableDeleteFragment(ISqlFragment target, ISqlFragment fromClause, ISqlFragment? whereClause)
    {
        Target = target;
        FromClause = fromClause;
        WhereClause = whereClause;
    }

    public string ToSql(ISqlContext context, SqlRenderMode renderMode = SqlRenderMode.Default)
    {
        return context.Dialect.RenderFragment(this, context);
    }
}