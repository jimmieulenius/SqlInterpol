using SqlInterpol.Configuration;
using SqlInterpol.Schema;

namespace SqlInterpol.Segments;

/// <summary>
/// Represents an explicitly aliased UPDATE statement, allowing dialects to transpile
/// the target table and its alias using their required syntax (e.g., UPDATE alias SET ... FROM table).
/// </summary>
/// <param name="target">The target entity being updated.</param>
public class SqlUpdateAsFragment(ISqlEntityBase target) : ISqlFragment, ISqlFeatureRequirement
{
    /// <summary>Gets the target entity being updated.</summary>
    public ISqlEntityBase Target { get; } = target;

    /// <inheritdoc />
    public SqlFeature RequiredFeature => SqlFeature.UpdateAs;

    /// <inheritdoc />
    public string FeatureName => "UPDATE AS";

    /// <inheritdoc />
    public string ToSql(ISqlContext context, SqlRenderMode mode = SqlRenderMode.Default)
    {
        return context.Dialect.RenderFragment(this, context);
    }
}