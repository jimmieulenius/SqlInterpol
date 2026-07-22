using SqlInterpol.Configuration;

namespace SqlInterpol.Segments;

/// <summary>
/// Represents a row-level locking clause (<c>FOR UPDATE</c> or <c>FOR SHARE</c>),
/// delegating dialect-specific rendering to <see cref="ISqlDialect.RenderFragment"/>.
/// </summary>
/// <param name="Mode">The locking mode to apply.</param>
public record SqlLockFragment(SqlLockMode Mode) : ISqlFragment, ISqlFeatureRequirement
{
    /// <inheritdoc />
    public SqlFeature RequiredFeature => Mode == SqlLockMode.Update ? SqlFeature.ForUpdate : SqlFeature.ForShare;

    /// <inheritdoc />
    public string FeatureName => Mode == SqlLockMode.Update ? "FOR UPDATE" : "FOR SHARE";

    /// <inheritdoc />
    public string ToSql(ISqlContext context, SqlRenderMode mode = SqlRenderMode.Default)
    {
        return context.Dialect.RenderFragment(this, context);
    }
}