using SqlInterpol.Rewriters;

namespace SqlInterpol.Dialects.PostgreSql;

public class PostgreSqlSyntaxRewriter : SqlSyntaxRewriterBase
{
    private SqlLockMode? _deferredLock;

    protected override bool TryRewriteLock(SqlLockFragment lockFrag, IReadOnlyList<SqlSegment> segments, List<SqlSegment> rewritten, ref int i)
    {
        _deferredLock = lockFrag.Mode;
        return true; 
    }

    protected override bool TryRewriteUpsert(SqlSegment segment, IReadOnlyList<SqlSegment> segments, List<SqlSegment> rewritten, ref int i)
    {
        return TryRewriteStandardOnConflict(segment, segments, rewritten, ref i);
    }

    protected override void ApplyDeferredTransforms(List<SqlSegment> rewritten, ISqlContext context)
    {
        if (_deferredLock == SqlLockMode.Update) rewritten.Add(new SqlSegment(SqlSegmentType.Literal, "\nFOR UPDATE"));
        else if (_deferredLock == SqlLockMode.Share) rewritten.Add(new SqlSegment(SqlSegmentType.Literal, "\nFOR SHARE"));
        base.ApplyDeferredTransforms(rewritten, context);
    }
}