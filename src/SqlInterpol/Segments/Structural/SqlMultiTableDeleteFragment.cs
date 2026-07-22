using SqlInterpol.Configuration;

namespace SqlInterpol.Segments;

/// <summary>
/// Represents a multi-table DELETE statement, delegating dialect-specific rendering
/// to <see cref="ISqlDialect.RenderFragment"/>.
/// </summary>
/// <param name="target">The target table fragment.</param>
/// <param name="fromClause">The FROM clause fragment.</param>
/// <param name="whereClause">The optional WHERE clause fragment.</param>
public class SqlMultiTableDeleteFragment(ISqlFragment target, ISqlFragment fromClause, ISqlFragment? whereClause) 
    : ISqlFragment, ISqlFeatureRequirement
{
    /// <inheritdoc />
    public SqlFeature RequiredFeature => SqlFeature.MultiTableDelete;

    /// <inheritdoc />
    public string FeatureName => "Multi-Table DELETE";

    /// <summary>Gets the target table fragment (the table from which rows are deleted).</summary>
    public ISqlFragment Target { get; } = target;

    /// <summary>Gets the FROM clause fragment (the joined source tables).</summary>
    public ISqlFragment FromClause { get; } = fromClause;

    /// <summary>Gets the optional WHERE clause fragment.</summary>
    public ISqlFragment? WhereClause { get; } = whereClause;

    /// <inheritdoc />
    public string ToSql(ISqlContext context, SqlRenderMode renderMode = SqlRenderMode.Default)
    {
        return context.Dialect.RenderFragment(this, context);
    }
}