using SqlInterpol.Configuration;
using SqlInterpol.Segments;

namespace SqlInterpol.Pipeline;

/// <summary>
/// The orchestrator that executes preprocessing and segment rewriting before final rendering.
/// </summary>
public class SqlPipeline(ISqlSegmentPreprocessor preprocessor, IEnumerable<ISqlSegmentRewriter> rewriters)
{
    private readonly ISqlSegmentPreprocessor _preprocessor = preprocessor;
    private readonly ISqlSegmentRewriter[] _rewriters = rewriters.ToArray();

    /// <summary>
    /// Preprocesses and refines raw SQL segments into a finalized, dialect-ready sequence.
    /// </summary>
    public IReadOnlyList<SqlSegment> Process(IReadOnlyList<SqlSegment> rawSegments, ISqlContext context)
    {
        // 1. Preprocessing & Tagging
        var segments = _preprocessor.Process(rawSegments, context);

        // 2. Generate O(1) State Snapshot
        var state = new SqlPipelineState(segments, context);

        // 3. Execute Segment Transformations
        for (int i = 0; i < _rewriters.Length; i++)
        {
            var rewriter = _rewriters[i];

            // Nanosecond bypass
            if (!rewriter.IsApplicable(state)) continue;

            segments = rewriter.Rewrite(segments, context);
        }

        return segments;
    }
}