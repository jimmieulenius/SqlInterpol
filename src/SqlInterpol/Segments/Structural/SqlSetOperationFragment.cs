using SqlInterpol.Configuration;

namespace SqlInterpol.Segments;

/// <summary>
/// Represents a set operation (UNION, INTERSECT, EXCEPT) combining two query fragments,
/// delegating dialect-specific rendering to <see cref="ISqlDialect.RenderFragment"/>.
/// </summary>
/// <param name="left">The left-hand query fragment.</param>
/// <param name="right">The right-hand query fragment.</param>
/// <param name="setOperator">The set operator to apply.</param>
public class SqlSetOperationFragment(ISqlFragment left, ISqlFragment right, SqlSetOperator setOperator) : ISqlFragment
{
    /// <summary>Gets the left-hand query fragment.</summary>
    public ISqlFragment Left { get; } = left;

    /// <summary>Gets the right-hand query fragment.</summary>
    public ISqlFragment Right { get; } = right;

    /// <summary>Gets the set operator to apply between <see cref="Left"/> and <see cref="Right"/>.</summary>
    public SqlSetOperator Operator { get; } = setOperator;

    /// <inheritdoc />
    public string ToSql(ISqlContext context, SqlRenderMode renderMode = SqlRenderMode.Default)
    {
        return context.Dialect.RenderFragment(this, context);
    }
}