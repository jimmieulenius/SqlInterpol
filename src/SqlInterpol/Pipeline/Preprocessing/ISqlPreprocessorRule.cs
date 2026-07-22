using SqlInterpol.Segments;

namespace SqlInterpol.Pipeline;

/// <summary>
/// A custom lexical analysis rule injected into the Preprocessor pipeline.
/// </summary>
public interface ISqlPreprocessorRule
{
    /// <summary>
    /// Processes the current segment. 
    /// </summary>
    /// <param name="segment">The current segment being processed, passed by reference.</param>
    /// <param name="segments">The complete list of segments in the current context.</param>
    /// <param name="index">The current processing index within the segments list.</param>
    /// <param name="state">The mutable state of the preprocessor pass.</param>
    /// <returns><see langword="true"/> if the rule fully handled the segment and the core pipeline should skip it; otherwise, <see langword="false"/>.</returns>
    bool Process(ref SqlSegment segment, IReadOnlyList<SqlSegment> segments, int index, SqlPreprocessorState state);
}