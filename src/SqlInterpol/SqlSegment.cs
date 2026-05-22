namespace SqlInterpol;

/// <summary>
/// Represents a single unit of parsed SQL content in the builder's internal segment pipeline.
/// The pipeline accumulates <see cref="SqlSegment"/> instances as SQL is appended, then renders
/// them into the final <see cref="SqlQueryResult"/> during the build phase.
/// </summary>
/// <param name="type">The kind of content this segment holds.</param>
/// <param name="value">The raw value — a string, fragment, parameter value, or reference object depending on <paramref name="type"/>.</param>
/// <param name="renderMode">Optional rendering hint that overrides the default render behaviour for this segment.</param>
/// <param name="tag">Optional tag set by the parser to carry dialect-validation metadata (e.g. <see cref="SqlSegmentTag"/>).</param>
public class SqlSegment(SqlSegmentType type, object? value, SqlRenderMode? renderMode = null, string? tag = null)
{
    /// <summary>Gets the kind of content this segment holds.</summary>
    public SqlSegmentType Type { get; } = type;
    /// <summary>Gets the raw value carried by this segment.</summary>
    public object? Value { get; } = value;
    /// <summary>Gets the optional render-mode hint that overrides default rendering behaviour.</summary>
    public SqlRenderMode? RenderMode { get; } = renderMode;
    /// <summary>Gets the optional parser tag used for dialect-capability validation at build time.</summary>
    public string? Tag { get; } = tag;
}