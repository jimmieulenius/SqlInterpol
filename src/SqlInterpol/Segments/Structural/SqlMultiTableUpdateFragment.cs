using SqlInterpol.Configuration;

namespace SqlInterpol.Segments;

/// <summary>
/// Represents a multi-table UPDATE statement, delegating dialect-specific rendering
/// to <see cref="ISqlDialect.RenderFragment"/>.
/// </summary>
/// <param name="target">The target table fragment.</param>
/// <param name="setClause">The SET clause fragment.</param>
/// <param name="fromClause">The FROM clause fragment.</param>
/// <param name="whereClause">The optional WHERE clause fragment.</param>
public class SqlMultiTableUpdateFragment(ISqlFragment target, ISqlFragment setClause, ISqlFragment fromClause, ISqlFragment? whereClause = null) 
    : ISqlFragment, ISqlFeatureRequirement
{
    /// <inheritdoc />
    public SqlFeature RequiredFeature => SqlFeature.MultiTableUpdate;

    /// <inheritdoc />
    public string FeatureName => "Multi-Table UPDATE";

    /// <summary>Gets the target table fragment (the table to be updated).</summary>
    public ISqlFragment Target { get; } = target;

    /// <summary>Gets the SET clause fragment.</summary>
    public ISqlFragment SetClause { get; } = setClause;

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