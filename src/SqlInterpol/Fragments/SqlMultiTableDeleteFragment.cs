
namespace SqlInterpol;

/// <summary>
/// Represents a multi-table DELETE statement, delegating dialect-specific rendering
/// to <see cref="ISqlDialect.RenderFragment"/>.
/// </summary>
public class SqlMultiTableDeleteFragment : ISqlFragment
{
    /// <summary>Gets the target table fragment (the table from which rows are deleted).</summary>
    public ISqlFragment Target { get; }

    /// <summary>Gets the FROM clause fragment (the joined source tables).</summary>
    public ISqlFragment FromClause { get; }

    /// <summary>Gets the optional WHERE clause fragment.</summary>
    public ISqlFragment? WhereClause { get; }

    /// <summary>
    /// Initializes a new <see cref="SqlMultiTableDeleteFragment"/>.
    /// </summary>
    /// <param name="target">The target table fragment.</param>
    /// <param name="fromClause">The FROM clause fragment.</param>
    /// <param name="whereClause">The optional WHERE clause fragment.</param>
    public SqlMultiTableDeleteFragment(ISqlFragment target, ISqlFragment fromClause, ISqlFragment? whereClause)
    {
        Target = target;
        FromClause = fromClause;
        WhereClause = whereClause;
    }

    /// <inheritdoc />
    public string ToSql(ISqlContext context, SqlRenderMode renderMode = SqlRenderMode.Default)
    {
        return context.Dialect.RenderFragment(this, context);
    }
}