using SqlInterpol.Parsing;

namespace SqlInterpol.Test.XvSql.Functions;

/// <summary>
/// A unified, high-performance AST rewriter that processes all custom Meta-SQL function strategies.
/// </summary>
public partial class XvSqlFunctionsRewriter : ISqlSegmentRewriter
{
    private readonly Dictionary<string, Func<SqlSegment, ISqlContext, SqlSegment>> _strategies = new(StringComparer.OrdinalIgnoreCase);

    public XvSqlFunctionsRewriter()
    {
        // Centralized registration registry
        InitializeStrategies();
    }

    public bool IsApplicable(ISqlCompilationState state)
    {
        foreach (var tag in _strategies.Keys)
        {
            if (state.HasTag(tag)) return true;
        }
        return false;
    }

    public IReadOnlyList<SqlSegment> Rewrite(IReadOnlyList<SqlSegment> segments, ISqlContext context)
    {
        var rewritten = new List<SqlSegment>(segments.Count);

        for (int i = 0; i < segments.Count; i++)
        {
            var seg = segments[i];
            bool matched = false;

            if (seg.Tags != null)
            {
                for (int t = 0; t < seg.Tags.Length; t++)
                {
                    if (_strategies.TryGetValue(seg.Tags[t], out var transform))
                    {
                        rewritten.Add(transform(seg, context));
                        matched = true;
                        break; 
                    }
                }
            }

            if (!matched)
            {
                rewritten.Add(seg);
            }
        }

        return rewritten;
    }

    private void InitializeStrategies()
    {
        // Call each partial file's initializer cleanly
        InitializeConcatWsStrategy();
        InitializeCastStrategy();
        // InitializeStringAggStrategy();
    }
    
    // Partial method hooks ensuring compilation safety across files
    private partial void InitializeConcatWsStrategy();
    private partial void InitializeCastStrategy();
}