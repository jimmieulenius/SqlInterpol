
namespace SqlInterpol;

/// <summary>
/// Represents a multi-table UPDATE statement, delegating dialect-specific rendering
/// to <see cref="ISqlDialect.RenderFragment"/>.
/// </summary>
public class SqlMultiTableUpdateFragment : ISqlFragment
{
    /// <summary>Gets the target table fragment (the table to be updated).</summary>
    public ISqlFragment Target { get; }

    /// <summary>Gets the SET clause fragment.</summary>
    public ISqlFragment SetClause { get; }

    /// <summary>Gets the FROM clause fragment (the joined source tables).</summary>
    public ISqlFragment FromClause { get; }

    /// <summary>Gets the optional WHERE clause fragment.</summary>
    public ISqlFragment? WhereClause { get; }

    /// <summary>
    /// Initializes a new <see cref="SqlMultiTableUpdateFragment"/>.
    /// </summary>
    /// <param name="target">The target table fragment.</param>
    /// <param name="setClause">The SET clause fragment.</param>
    /// <param name="fromClause">The FROM clause fragment.</param>
    /// <param name="whereClause">The optional WHERE clause fragment.</param>
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

    /// <inheritdoc />
    public string ToSql(ISqlContext context, SqlRenderMode renderMode = SqlRenderMode.Default)
    {
        return context.Dialect.RenderFragment(this, context);
    }
}