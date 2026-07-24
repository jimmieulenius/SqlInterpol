using SqlInterpol.Configuration;

namespace SqlInterpol.Segments;

/// <summary>
/// Represents an UPDATE statement transformed into a Common Table Expression fallback sequence.
/// </summary>
/// <param name="alias">The alias mapped to the CTE.</param>
/// <param name="subquery">The nested subquery forming the CTE.</param>
/// <param name="setClause">The SET clause.</param>
/// <param name="whereClause">The optional WHERE clause.</param>
public class SqlUpdateCteFragment(string alias, ISqlQueryFragment subquery, ISqlFragment setClause, ISqlFragment? whereClause) 
    : ISqlFragment
{
    /// <summary>Gets the alias for the CTE.</summary>
    public string Alias { get; } = alias;

    /// <summary>Gets the subquery definition.</summary>
    public ISqlQueryFragment Subquery { get; } = subquery;

    /// <summary>Gets the SET clause.</summary>
    public ISqlFragment SetClause { get; } = setClause;

    /// <summary>Gets the optional WHERE clause.</summary>
    public ISqlFragment? WhereClause { get; } = whereClause;

    /// <inheritdoc />
    public string ToSql(ISqlContext context, SqlRenderMode mode = SqlRenderMode.Default)
    {
        return context.Dialect.RenderFragment(this, context);
    }
}