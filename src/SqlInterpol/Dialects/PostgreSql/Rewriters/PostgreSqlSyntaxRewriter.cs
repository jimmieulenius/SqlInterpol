using System.Collections.Generic;
using SqlInterpol.Configuration;
using SqlInterpol.Pipeline;
using SqlInterpol.Segments;

namespace SqlInterpol.Dialects.PostgreSql;

/// <summary>
/// A structural rewriter for PostgreSQL that handles standard ON CONFLICT upsert syntax
/// and safely repositions deferred locking hints (FOR UPDATE / FOR SHARE).
/// </summary>
public class PostgreSqlSyntaxRewriter : SqlSyntaxRewriterBase
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
        _deferredLock = lockFrag.Mode;
        return true; 
    }

    /// <inheritdoc />
    protected override bool TryRewriteUpsert(SqlSegment segment, IReadOnlyList<SqlSegment> segments, List<SqlSegment> rewritten, ref int i)
    {
        return TryRewriteStandardOnConflict(segment, segments, rewritten, ref i);
    }

    /// <inheritdoc />
    protected override void ApplyDeferredTransforms(List<SqlSegment> rewritten, ISqlContext context)
    {
        if (_deferredLock == SqlLockMode.Update) rewritten.Add(new SqlSegment(SqlSegmentType.Literal, "\nFOR UPDATE"));
        else if (_deferredLock == SqlLockMode.Share) rewritten.Add(new SqlSegment(SqlSegmentType.Literal, "\nFOR SHARE"));
        
        base.ApplyDeferredTransforms(rewritten, context);
    }
}