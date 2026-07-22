using SqlInterpol.Configuration;
using SqlInterpol.Schema;

namespace SqlInterpol.Segments;

/// <summary>
/// Represents an inline updatable subquery statement view block (e.g., UPDATE (SELECT ...) AS alias SET ...).
/// </summary>
/// <param name="subquery">The nested query targeted for update.</param>
/// <param name="alias">The assigned alias.</param>
/// <param name="setClause">The SET clause.</param>
/// <param name="whereClause">The optional WHERE clause.</param>
public class SqlUpdateSubqueryFragment(ISqlQuery subquery, string alias, ISqlFragment setClause, ISqlFragment? whereClause) 
    : ISqlFragment
{
    /// <summary>Gets the inline subquery.</summary>
    public ISqlQuery Subquery { get; } = subquery;

    /// <summary>Gets the assigned alias.</summary>
    public string Alias { get; } = alias;

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