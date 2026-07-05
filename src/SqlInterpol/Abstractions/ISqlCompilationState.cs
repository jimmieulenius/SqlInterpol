namespace SqlInterpol.Parsing;

/// <summary>
/// A fast, read-only snapshot of the query's structural metadata, used to achieve 
/// O(1) short-circuiting in the compiler pipeline.
/// </summary>
public interface ISqlCompilationState
{
    /// <summary>
    /// Gets the active SQL context containing dialect capabilities, options, and parameters.
    /// </summary>
    ISqlContext Context { get; }

    /// <summary>
    /// Returns true if the query contains at least one segment with the specified tag.
    /// Evaluates in O(1) time.
    /// </summary>
    bool HasTag(string tag);
}