using SqlInterpol.Parsing;

namespace SqlInterpol.Dialects.MySql;

/// <summary>
/// Rewrites ON CONFLICT into ON DUPLICATE KEY UPDATE and transpiles aliased UPDATE structures.
/// </summary>
public class MySqlSyntaxRewriter : ISqlSegmentRewriter
{
    /// <inheritdoc />
    public bool IsApplicable(ISqlCompilationState state) => true;

    /// <inheritdoc />
    public IReadOnlyList<SqlSegment> Rewrite(IReadOnlyList<SqlSegment> segments, ISqlContext context)
    {
        var rewritten = new List<SqlSegment>(segments.Count);
        SqlLockMode? deferredLock = null;

        for (int i = 0; i < segments.Count; i++)
        {
            var segment = segments[i];

            if (segment.Type == SqlSegmentType.Raw && segment.Value is SqlLockFragment lockFrag)
            {
                deferredLock = lockFrag.Mode;
                continue;
            }

            bool isOnConflict = segment.HasTag(SqlSegmentTag.OnConflictKeyword) || 
                               (segment.Type == SqlSegmentType.Literal && segment.Value is string s1 && s1.Contains("ON CONFLICT", StringComparison.OrdinalIgnoreCase));

            if (isOnConflict)
            {
                SqlSetFragment? setFrag = null;
                int setFragIndex = -1;
                int lookahead = 1;

                while (i + lookahead < segments.Count)
                {
                    var next = segments[i + lookahead];
                    
                    if (next.Value is SqlSetFragment sf)
                    {
                        setFrag = sf;
                        setFragIndex = i + lookahead;
                        break;
                    }
                    
                    lookahead++;
                }

                if (setFrag != null)
                {
                    if (segment.Value is string text)
                    {
                        int idx = text.LastIndexOf("ON CONFLICT", StringComparison.OrdinalIgnoreCase);
                        if (idx > 0)
                        {
                            var precedingText = text[..idx].TrimEnd();
                            if (precedingText.Length > 0)
                            {
                                rewritten.Add(new SqlSegment(SqlSegmentType.Literal, precedingText));
                            }
                        }
                    }

                    // FIX: Removed the trailing space here!
                    rewritten.Add(new SqlSegment(SqlSegmentType.Literal, "\nON DUPLICATE KEY UPDATE"));
                    rewritten.Add(new SqlSegment(SqlSegmentType.Raw, new MySqlUpdateFragment(setFrag)));

                    i = setFragIndex; 
                    continue;
                }
            }
            
            rewritten.Add(segment);
        }

        if (deferredLock == SqlLockMode.Update)
            rewritten.Add(new SqlSegment(SqlSegmentType.Literal, "\nFOR UPDATE"));
        else if (deferredLock == SqlLockMode.Share)
            rewritten.Add(new SqlSegment(SqlSegmentType.Literal, "\nFOR SHARE"));

        int upAsIdx = rewritten.FindIndex(s => s.Type == SqlSegmentType.Raw && s.Value is SqlUpdateAsFragment);
        bool isAlreadyElevated = rewritten.Any(s => s.Type == SqlSegmentType.Raw && (s.Value is SqlUpdateCteFragment || s.Value is SqlUpdateSubqueryFragment));

        if (upAsIdx > -1 && !isAlreadyElevated)
        {
            var upAsFrag = (SqlUpdateAsFragment)rewritten[upAsIdx].Value!;
            var targetEntity = upAsFrag.Target;

            int setFragIdx = rewritten.FindIndex(upAsIdx + 1, s => s.Value is SqlSetFragment);
            int whereKeywordIdx = rewritten.FindIndex(upAsIdx + 1, s => s.HasTag(SqlSegmentTag.WhereKeyword));

            if (setFragIdx > -1)
            {
                var setFrag = (SqlSetFragment)rewritten[setFragIdx].Value!;
                var targetFrag = new SqlSegmentCollectionFragment([new SqlSegment(SqlSegmentType.Reference, targetEntity, SqlRenderMode.AliasOnly)]);
                
                var fromFrag = new SqlSegmentCollectionFragment([
                    new SqlSegment(SqlSegmentType.Reference, targetEntity, SqlRenderMode.BaseName),
                    new SqlSegment(SqlSegmentType.Literal, " AS "),
                    new SqlSegment(SqlSegmentType.Reference, targetEntity, SqlRenderMode.AliasOnly)
                ]);

                SqlSegmentCollectionFragment? whereClause = null;
                if (whereKeywordIdx > -1)
                {
                    whereClause = new SqlSegmentCollectionFragment(rewritten.Skip(whereKeywordIdx + 1).ToList());
                }

                rewritten.RemoveRange(upAsIdx, rewritten.Count - upAsIdx);
                rewritten.Add(new SqlSegment(SqlSegmentType.Raw, new SqlMultiTableUpdateFragment(targetFrag, setFrag, fromFrag, whereClause)));
            }
        }

        return rewritten;
    }
}