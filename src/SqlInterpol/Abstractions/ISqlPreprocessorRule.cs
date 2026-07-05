namespace SqlInterpol.Parsing;

/// <summary>
/// A custom lexical analysis rule injected into the Preprocessor pipeline.
/// </summary>
public interface ISqlPreprocessorRule
{
    /// <summary>
    /// Processes the current segment. 
    /// </summary>
    /// <returns><see langword="true"/> if the rule fully handled the segment and the core pipeline should skip it; otherwise, <see langword="false"/>.</returns>
    bool Process(ref SqlSegment segment, IReadOnlyList<SqlSegment> segments, int index, SqlPreprocessorState state);
}