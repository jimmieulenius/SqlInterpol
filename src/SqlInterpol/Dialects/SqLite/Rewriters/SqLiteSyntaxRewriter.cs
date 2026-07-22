using SqlInterpol.Pipeline;
using SqlInterpol.Segments;

namespace SqlInterpol.Dialects.SqLite;

/// <summary>
/// A structural rewriter for SQLite that handles standard ON CONFLICT upsert transformations.
/// </summary>
public class SqLiteSyntaxRewriter : SqlSyntaxRewriterBase
{
    /// <inheritdoc />
    protected override bool TryRewriteUpsert(SqlSegment segment, IReadOnlyList<SqlSegment> segments, List<SqlSegment> rewritten, ref int i)
    {
        return TryRewriteStandardOnConflict(segment, segments, rewritten, ref i);
    }
}