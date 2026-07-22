using System;
using System.Collections.Generic;
using SqlInterpol.Configuration;
using SqlInterpol.Pipeline;
using SqlInterpol.Segments;

namespace SqlInterpol.Dialects.Firebird;

/// <summary>
/// A structural rewriter for Firebird that handles query pagination (FIRST/SKIP) 
/// and safely repositions deferred locking hints (WITH LOCK) to the end of the query.
/// </summary>
public class FirebirdSyntaxRewriter : SqlSyntaxRewriterBase
{
    private SqlLockMode? _deferredLock;

    /// <inheritdoc />
    public override IReadOnlyList<SqlSegment> Rewrite(IReadOnlyList<SqlSegment> segments, ISqlContext context)
    {
        _deferredLock = null; // Ensure clean state per pass across multiple queries
        return base.Rewrite(segments, context);
    }

    /// <inheritdoc />
    protected override bool TryRewriteLock(SqlLockFragment lockFrag, IReadOnlyList<SqlSegment> segments, List<SqlSegment> rewritten, ref int i)
    {
        if (lockFrag.Mode == SqlLockMode.Update)
        {
            _deferredLock = lockFrag.Mode;
            return true;
        }
        return false;
    }

    /// <inheritdoc />
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

    /// <inheritdoc />
    protected override void ApplyDeferredTransforms(List<SqlSegment> rewritten, ISqlContext context)
    {
        if (_deferredLock == SqlLockMode.Update) rewritten.Add(new SqlSegment(SqlSegmentType.Literal, "\nWITH LOCK"));
        base.ApplyDeferredTransforms(rewritten, context);
    }
}