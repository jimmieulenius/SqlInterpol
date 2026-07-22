using SqlInterpol.Configuration;
using SqlInterpol.Pipeline;
using SqlInterpol.Segments;

namespace SqlInterpol.Dialects.MySql;

/// <summary>
/// A structural rewriter for MySQL and MariaDB that transforms standard upsert syntax into 
/// <c>ON DUPLICATE KEY UPDATE</c>, manages deferred lock hints, and restructures multi-table updates.
/// </summary>
public class MySqlSyntaxRewriter : SqlSyntaxRewriterBase
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
        bool isOnConflict = segment.HasTag(SqlSegmentTag.OnConflictKeyword) || 
            (segment.Type == SqlSegmentType.Literal && segment.Value is string s1 && SqlRewriterHelpers.ContainsKeyword(s1, SqlKeyword.OnConflict.Value));

        if (!isOnConflict) return false;

        SqlSetFragment? setFrag = null;
        int setFragIndex = -1;
        int lookahead = 1;

        while (i + lookahead < segments.Count)
        {
            var next = segments[i + lookahead];
            if (next.Value is SqlSetFragment sf) { setFrag = sf; setFragIndex = i + lookahead; break; }
            lookahead++;
        }

        if (setFrag != null)
        {
            if (segment.Value is string text)
            {
                int idx = text.LastIndexOf(SqlKeyword.OnConflict.Value, StringComparison.OrdinalIgnoreCase);
                if (idx > 0)
                {
                    var precedingText = text[..idx].TrimEnd();
                    if (precedingText.Length > 0) rewritten.Add(new SqlSegment(SqlSegmentType.Literal, precedingText));
                }
            }

            rewritten.Add(new SqlSegment(SqlSegmentType.Literal, "\nON DUPLICATE KEY UPDATE"));
            rewritten.Add(new SqlSegment(SqlSegmentType.Raw, new MySqlUpdateFragment(setFrag)));

            i = setFragIndex; 
            return true;
        }
        return false;
    }

    /// <inheritdoc />
    protected override void ApplyDeferredTransforms(List<SqlSegment> rewritten, ISqlContext context)
    {
        if (_deferredLock == SqlLockMode.Update) rewritten.Add(new SqlSegment(SqlSegmentType.Literal, "\nFOR UPDATE"));
        else if (_deferredLock == SqlLockMode.Share) rewritten.Add(new SqlSegment(SqlSegmentType.Literal, "\nFOR SHARE"));
        
        base.ApplyDeferredTransforms(rewritten, context);
    }

    /// <inheritdoc />
    protected override SqlMultiTableUpdateFragment? CreateMultiTableUpdate(SqlUpdateAsFragment upAsFrag, SqlSetFragment setFrag, List<SqlSegment> rewritten, int whereKeywordIdx, ISqlContext context)
    {
        var targetEntity = upAsFrag.Target;
        var targetFrag = new SqlSegmentCollectionFragment([new SqlSegment(SqlSegmentType.Reference, targetEntity, SqlRenderMode.AliasOnly)]);
        
        var fromFrag = new SqlSegmentCollectionFragment([
            new SqlSegment(SqlSegmentType.Reference, targetEntity, SqlRenderMode.BaseName),
            new SqlSegment(SqlSegmentType.Literal, " AS "),
            new SqlSegment(SqlSegmentType.Reference, targetEntity, SqlRenderMode.AliasOnly)
        ]);

        SqlSegmentCollectionFragment? whereClause = null;
        if (whereKeywordIdx > -1) whereClause = new SqlSegmentCollectionFragment(rewritten.Skip(whereKeywordIdx + 1).ToList());

        return new SqlMultiTableUpdateFragment(targetFrag, setFrag, fromFrag, whereClause);
    }
}