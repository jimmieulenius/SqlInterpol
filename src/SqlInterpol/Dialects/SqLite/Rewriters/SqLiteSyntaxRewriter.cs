using SqlInterpol.Rewriters;

namespace SqlInterpol.Dialects.SqLite;

public class SqLiteSyntaxRewriter : SqlSyntaxRewriterBase
{
    protected override bool TryRewriteUpsert(SqlSegment segment, IReadOnlyList<SqlSegment> segments, List<SqlSegment> rewritten, ref int i)
    {
        return TryRewriteStandardOnConflict(segment, segments, rewritten, ref i);
    }
}