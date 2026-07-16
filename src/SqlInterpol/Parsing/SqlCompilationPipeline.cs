namespace SqlInterpol.Parsing;

/// <summary>
/// The grand orchestrator that executes Lexical Analysis (Preprocessor) and 
/// Semantic Parsing (Rewriters) before handing off to the Dialect for code generation.
/// </summary>
public class SqlCompilationPipeline(ISqlSegmentPreprocessor preprocessor, IEnumerable<ISqlSegmentRewriter> rewriters)
{
    private readonly ISqlSegmentPreprocessor _preprocessor = preprocessor;
    private readonly ISqlSegmentRewriter[] _rewriters = rewriters.ToArray();

    public IReadOnlyList<SqlSegment> Compile(IReadOnlyList<SqlSegment> rawSegments, ISqlContext context)
    {
        // 1. Lexical Analysis & Tagging
        var segments = _preprocessor.Process(rawSegments, context);

        // 2. Generate O(1) State Snapshot (Now passing context!)
        var state = new SqlCompilationState(segments, context);

        // 3. Execute AST Transformations
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