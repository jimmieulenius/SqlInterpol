namespace SqlInterpol;

/// <summary>
/// Represents an UPDATE statement transformed into a Common Table Expression fallback sequence.
/// </summary>
public class SqlUpdateCteFragment : ISqlFragment
{
    public string Alias { get; }
    
    public ISqlQueryFragment Subquery { get; }
    public ISqlFragment SetClause { get; }
    public ISqlFragment? WhereClause { get; }

    public SqlUpdateCteFragment(string alias, ISqlQueryFragment subquery, ISqlFragment setClause, ISqlFragment? whereClause)
    {
        Alias = alias;
        Subquery = subquery;
        SetClause = setClause;
        WhereClause = whereClause;
    }

    public string ToSql(ISqlContext context, SqlRenderMode mode = SqlRenderMode.Default)
    {
        return context.Dialect.RenderFragment(this, context);
    }
}