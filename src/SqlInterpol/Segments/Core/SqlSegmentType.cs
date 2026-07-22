namespace SqlInterpol.Segments;

/// <summary>
/// Identifies the kind of content held by a <see cref="SqlSegment"/>, controlling how the
/// rendering pipeline processes each segment.
/// </summary>
public enum SqlSegmentType
{
    /// <summary>A plain SQL text literal emitted verbatim into the output.</summary>
    Literal,
    /// <summary>A typed column projection resolved from an entity expression.</summary>
    Projection,
    /// <summary>A typed table or column reference that renders according to the active dialect.</summary>
    Reference,
    /// <summary>A composable <see cref="ISqlFragment"/> that renders itself.</summary>
    Fragment,
    /// <summary>A parameterized value that will be extracted as a <c>DbParameter</c>.</summary>
    Parameter,
    /// <summary>A raw, unescaped SQL string inserted directly into the output without parameterization.</summary>
    Raw,
    /// <summary>A placeholder for a runtime parameter in a pre-compiled SQL template that must be resolved before rendering.</summary>
    Argument,
    /// <summary>
    /// A C# object from an interpolation hole that has not yet been 
    /// evaluated or mapped by the SqlSegmentPreprocessor.
    /// </summary>
    Unresolved
}