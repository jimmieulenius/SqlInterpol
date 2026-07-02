using SqlInterpol.Parsing;
using SqlInterpol.Rewriters;

namespace SqlInterpol.Dialects.Firebird;

public class FirebirdSyntaxRewriter : SqlSyntaxRewriterBase
{
    private SqlLockMode? _deferredLock;

    public override IReadOnlyList<SqlSegment> Rewrite(IReadOnlyList<SqlSegment> segments, ISqlContext context)
    {
        _deferredLock = null; // Ensure clean state per pass
        return base.Rewrite(segments, context);
    }

    protected override bool TryRewriteLock(SqlLockFragment lockFrag, IReadOnlyList<SqlSegment> segments, List<SqlSegment> rewritten, ref int i)
    {
        if (lockFrag.Mode == SqlLockMode.Update)
        {
            _deferredLock = lockFrag.Mode;
            return true;
        }
        return false;
    }

    protected override bool TryRewritePaging(SqlSegment segment, IReadOnlyList<SqlSegment> segments, List<SqlSegment> rewritten, ref int i)
    {
        if (!segment.HasTag(SqlSegmentTag.Paging) || !SqlRewriterHelpers.TryExtractPagingParameters(segments, i, out var limitParam, out var offsetParam, out int nextIndex)) return false;

        if (segment.Value is string pagingValue)
        {
            int index = pagingValue.LastIndexOf(SqlKeyword.Limit.Value, StringComparison.OrdinalIgnoreCase);
            if (index > -1) rewritten.Add(new SqlSegment(SqlSegmentType.Literal, pagingValue[..index]));
        }

        rewritten.Add(new SqlSegment(SqlSegmentType.Literal, "FIRST "));
        rewritten.Add(limitParam); 
        rewritten.Add(new SqlSegment(SqlSegmentType.Literal, " SKIP "));
        rewritten.Add(offsetParam); 

        i = nextIndex;
        return true;
    }

    protected override void ApplyDeferredTransforms(List<SqlSegment> rewritten, ISqlContext context)
    {
        if (_deferredLock == SqlLockMode.Update) rewritten.Add(new SqlSegment(SqlSegmentType.Literal, "\nWITH LOCK"));
        base.ApplyDeferredTransforms(rewritten, context);
    }
}