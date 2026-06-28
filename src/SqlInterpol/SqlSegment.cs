namespace SqlInterpol;

/// <summary>
/// Represents a single tokenized piece of the SQL query timeline.
/// </summary>
public class SqlSegment(SqlSegmentType type, object? value, SqlRenderMode? renderMode = null, params string[]? tags)
{
    /// <summary>Gets the kind of content this segment holds.</summary>
    public SqlSegmentType Type { get; } = type;
    /// <summary>Gets the raw value carried by this segment.</summary>
    public object? Value { get; } = value;
    /// <summary>Gets the optional render-mode hint that overrides default rendering behaviour.</summary>
    public SqlRenderMode? RenderMode { get; } = renderMode;

    /// <summary>
    /// Semantic tags assigned to this segment by the preprocessor. 
    /// Allows pipeline rewriters to easily identify and target specific segments.
    /// </summary>
    public string[]? Tags { get; } = tags != null && tags.Length > 0 ? tags : null;

    /// <summary>
    /// Evaluates in O(N) time (where N is the number of tags on this specific segment, usually 1-3).
    /// </summary>
    public bool HasTag(string tag)
    {
        if (Tags == null) return false;
        for (int i = 0; i < Tags.Length; i++)
        {
            if (string.Equals(Tags[i], tag, StringComparison.OrdinalIgnoreCase)) return true;
        }
        return false;
    }
}