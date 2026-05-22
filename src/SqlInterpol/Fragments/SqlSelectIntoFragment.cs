namespace SqlInterpol;

/// <summary>
/// Represents a SELECT INTO statement, delegating dialect-specific rendering to
/// <see cref="ISqlDialect.RenderFragment"/>.
/// </summary>
/// <param name="targetTable">The destination table for the new rows.</param>
/// <param name="sourceSegments">The raw segments of the SELECT source query.</param>
/// <param name="intoSegmentIndex">The index within <paramref name="sourceSegments"/> of the INTO keyword segment.</param>
public class SqlSelectIntoFragment(object targetTable, IReadOnlyList<SqlSegment> sourceSegments, int intoSegmentIndex) : ISqlFragment
{
    /// <summary>Gets the destination table for the SELECT INTO statement.</summary>
    public object TargetTable { get; } = targetTable;

    /// <summary>Gets the raw segments of the SELECT source query.</summary>
    public IReadOnlyList<SqlSegment> SourceSegments { get; } = sourceSegments;

    /// <summary>Gets the index of the INTO keyword segment within <see cref="SourceSegments"/>.</summary>
    public int IntoSegmentIndex { get; } = intoSegmentIndex;

    /// <inheritdoc />
    public string ToSql(ISqlContext context, SqlRenderMode renderMode = SqlRenderMode.Default)
    {
        return context.Dialect.RenderFragment(this, context);
    }
}