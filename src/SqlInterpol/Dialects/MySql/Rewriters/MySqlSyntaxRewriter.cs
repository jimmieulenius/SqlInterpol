using SqlInterpol.Parsing;
using SqlInterpol.Rewriters;

namespace SqlInterpol.Dialects.MySql;

public class MySqlSyntaxRewriter : SqlSyntaxRewriterBase
{
    private SqlLockMode? _deferredLock;

    protected override bool TryRewriteLock(SqlLockFragment lockFrag, IReadOnlyList<SqlSegment> segments, List<SqlSegment> rewritten, ref int i)
    {
        _deferredLock = lockFrag.Mode;
        return true; 
    }

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

    protected override void ApplyDeferredTransforms(List<SqlSegment> rewritten, ISqlContext context)
    {
        if (_deferredLock == SqlLockMode.Update) rewritten.Add(new SqlSegment(SqlSegmentType.Literal, "\nFOR UPDATE"));
        else if (_deferredLock == SqlLockMode.Share) rewritten.Add(new SqlSegment(SqlSegmentType.Literal, "\nFOR SHARE"));
        base.ApplyDeferredTransforms(rewritten, context);
    }

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