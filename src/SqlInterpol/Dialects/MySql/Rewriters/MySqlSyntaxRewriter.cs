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
        int doIdx = -1;
        int lookahead = 1;

        if (segment.Type == SqlSegmentType.Literal && segment.Value is string s_do)
        {
            if (SqlRewriterHelpers.ContainsKeyword(s_do, SqlKeyword.Do.Value) || s_do.Contains("DO UPDATE SET", StringComparison.OrdinalIgnoreCase) || s_do.Contains("DO NOTHING", StringComparison.OrdinalIgnoreCase))
                doIdx = i;
        }

        while (i + lookahead < segments.Count)
        {
            var next = segments[i + lookahead];
            
            bool isDo = next.HasTag(SqlSegmentTag.DoUpdateSetKeyword) || 
                        (next.Type == SqlSegmentType.Literal && next.Value is string s2 && SqlRewriterHelpers.ContainsKeyword(s2, SqlKeyword.Do.Value));
            
            if (isDo && doIdx == -1) doIdx = i + lookahead;
            if (next.Value is SqlSetFragment sf) { setFrag = sf; setFragIndex = i + lookahead; break; }
            lookahead++;
        }

        if (doIdx > -1 || setFrag != null)
        {
            var doSegment = doIdx > -1 ? segments[doIdx] : segment;
            bool isDoNothing = false;
            int endDoIdx = doIdx > -1 ? doIdx : i;

            if (doSegment.Value is string doText)
            {
                if (doText.Contains("DO NOTHING", StringComparison.OrdinalIgnoreCase))
                {
                    isDoNothing = true;
                }
                else if (doText.TrimEnd().EndsWith("DO", StringComparison.OrdinalIgnoreCase) || doText.Trim().Equals("DO", StringComparison.OrdinalIgnoreCase))
                {
                    int nextIdx = endDoIdx + 1;
                    while (nextIdx < segments.Count && segments[nextIdx].Type == SqlSegmentType.Literal && string.IsNullOrWhiteSpace(segments[nextIdx].Value as string)) nextIdx++;
                    
                    if (nextIdx < segments.Count && segments[nextIdx].Type == SqlSegmentType.Literal && segments[nextIdx].Value is string nextText)
                    {
                        if (nextText.TrimStart().StartsWith("NOTHING", StringComparison.OrdinalIgnoreCase))
                        {
                            isDoNothing = true;
                            endDoIdx = nextIdx;
                        }
                    }
                }
            }

            if (isDoNothing)
            {
                 for (int k = rewritten.Count - 1; k >= 0; k--)
                 {
                     if (rewritten[k].HasTag(SqlSegmentTag.InsertKeyword) || 
                        (rewritten[k].Type == SqlSegmentType.Literal && (rewritten[k].Value as string)?.IndexOf(SqlKeyword.Insert.Value, StringComparison.OrdinalIgnoreCase) >= 0))
                     {
                         var insText = rewritten[k].Value as string;
                         if (insText != null && insText.IndexOf("INSERT IGNORE", StringComparison.OrdinalIgnoreCase) < 0)
                         {
                             string replaced = insText.Replace("INSERT INTO", "INSERT IGNORE INTO", StringComparison.OrdinalIgnoreCase);
                             if (replaced == insText) replaced = insText.Replace("INSERT", "INSERT IGNORE", StringComparison.OrdinalIgnoreCase);
                             rewritten[k] = new SqlSegment(SqlSegmentType.Literal, replaced, rewritten[k].RenderMode, rewritten[k].Tags);
                         }
                         break;
                     }
                 }
            }

            if (segment.Value is string text)
            {
                int idx = text.LastIndexOf(SqlKeyword.OnConflict.Value, StringComparison.OrdinalIgnoreCase);
                if (idx > 0)
                {
                    // FIX: Full TrimEnd() removes the preceding \n to prevent \n\n!
                    var precedingText = text[..idx].TrimEnd(); 
                    if (precedingText.Length > 0) rewritten.Add(new SqlSegment(SqlSegmentType.Literal, precedingText));
                }
            }
            
            if (!isDoNothing && setFrag != null)
            {
                rewritten.Add(new SqlSegment(SqlSegmentType.Literal, "\nON DUPLICATE KEY UPDATE"));
                rewritten.Add(new SqlSegment(SqlSegmentType.Raw, new MySqlUpdateFragment(setFrag)));
                i = setFragIndex;
                return true;
            }

            if (isDoNothing)
            {
                string trailing = "";
                string endText = segments[endDoIdx].Value as string ?? "";
                int nothingIdx = endText.IndexOf("NOTHING", StringComparison.OrdinalIgnoreCase);
                if (nothingIdx > -1) trailing = endText[(nothingIdx + 7)..];

                // FIX: Full TrimStart() prevents \n\n on trailing segments!
                trailing = trailing.TrimStart();
                if (!string.IsNullOrWhiteSpace(trailing))
                {
                    rewritten.Add(new SqlSegment(SqlSegmentType.Literal, "\n" + trailing));
                }

                i = endDoIdx;
                return true;
            }
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