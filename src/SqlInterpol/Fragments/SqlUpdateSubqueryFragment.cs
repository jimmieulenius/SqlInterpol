namespace SqlInterpol;

/// <summary>
/// Represents an inline updatable subquery statement view block (e.g., UPDATE (SELECT ...) AS alias SET ...).
/// </summary>
public class SqlUpdateSubqueryFragment : ISqlFragment
{
    // FIX: Updated subquery tracking type mapping to bind with ISqlQueryFragment
    public ISqlQueryFragment Subquery { get; }
    public string Alias { get; }
    public ISqlFragment SetClause { get; }
    public ISqlFragment? WhereClause { get; }

    public SqlUpdateSubqueryFragment(ISqlQueryFragment subquery, string alias, ISqlFragment setClause, ISqlFragment? whereClause)
    {
        Subquery = subquery;
        Alias = alias;
        SetClause = setClause;
        WhereClause = whereClause;
    }

    public string ToSql(ISqlContext context, SqlRenderMode mode = SqlRenderMode.Default)
    {
        return context.Dialect.RenderFragment(this, context);
    }
}