namespace SqlInterpol;

/// <summary>
/// Represents an explicitly aliased DELETE statement, allowing dialects to transpile
/// the target table and its alias using their required syntax (e.g., DELETE alias FROM table).
/// </summary>
public class SqlDeleteAsFragment(ISqlEntityBase target) : ISqlFragment, ISqlFeatureRequirement, ISqlSwappableFragment
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

    /// <inheritdoc />
    public ISqlFragment Swap(Dictionary<ISqlReference, ISqlEntityBase> entityMap, IReadOnlyDictionary<string, Func<object, object?>>? argumentGetters, object? arguments)
    {
        var mappedTarget = Target;

        if (entityMap.TryGetValue(Target.Reference, out var realEntity))
        {
            mappedTarget = realEntity;
        }
        
        return new SqlDeleteAsFragment(mappedTarget);
    }
}