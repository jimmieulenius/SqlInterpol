namespace SqlInterpol;

/// <summary>
/// Represents an AST node that contains a nested collection of SQL segments.
/// Implementing this allows the engine to recursively process inner queries without reflection.
/// </summary>
public interface ISqlSegmentContainer
{
    IReadOnlyList<SqlSegment> Segments { get; }
}