using SqlInterpol.Configuration;
using SqlInterpol.Schema;

namespace SqlInterpol.Segments;

/// <summary>
/// Represents an explicitly aliased DELETE statement, allowing dialects to transpile
/// the target table and its alias using their required syntax (e.g., DELETE alias FROM table).
/// </summary>
/// <param name="target">The target entity being deleted.</param>
public class SqlDeleteAsFragment(ISqlEntityBase target) : ISqlFragment, ISqlFeatureRequirement
{
    /// <summary>Gets the target entity being deleted.</summary>
    public ISqlEntityBase Target { get; } = target;

    /// <inheritdoc />
    public SqlFeature RequiredFeature => SqlFeature.DeleteAs;

    /// <inheritdoc />
    public string FeatureName => "DELETE AS";

    /// <inheritdoc />
    public string ToSql(ISqlContext context, SqlRenderMode mode = SqlRenderMode.Default)
    {
        return context.Dialect.RenderFragment(this, context);
    }
}