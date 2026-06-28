using SqlInterpol.Parsing;

namespace SqlInterpol;

/// <summary>
/// Represents a fast, discrete transformation step in the SQL compilation pipeline.
/// </summary>
public interface ISqlSegmentRewriter
{
    /// <summary>
    /// Evaluates in O(1) time whether this rewriter should execute, based on tags 
    /// collected during the initial text preprocessing phase.
    /// </summary>
    bool IsApplicable(ISqlCompilationState state);

    /// <summary>
    /// Applies structural transformations to the timeline. Must return the original 
    /// list reference if no modifications are made to avoid GC allocations.
    /// </summary>
    IReadOnlyList<SqlSegment> Rewrite(IReadOnlyList<SqlSegment> segments, ISqlContext context);
}