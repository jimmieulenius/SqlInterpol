
namespace SqlInterpol;

public class SqlMultiTableUpdateFragment : ISqlFragment
{
    public ISqlFragment Target { get; }
    public ISqlFragment SetClause { get; }
    public ISqlFragment FromClause { get; }
    public ISqlFragment? WhereClause { get; }

    public SqlMultiTableUpdateFragment(
        ISqlFragment target, 
        ISqlFragment setClause, 
        ISqlFragment fromClause, 
        ISqlFragment? whereClause = null)
    {
        Target = target;
        SetClause = setClause;
        FromClause = fromClause;
        WhereClause = whereClause;
    }

    public string ToSql(ISqlContext context, SqlRenderMode renderMode = SqlRenderMode.Default)
    {
        return context.Dialect.RenderFragment(this, context);
    }
}