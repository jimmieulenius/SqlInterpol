using SqlInterpol.Parsing;
using SqlInterpol.Rewriters;

namespace SqlInterpol.Dialects.SqlServer;

public class SqlServerSyntaxRewriter : SqlSyntaxRewriterBase
{
    protected override string ProcessLiteral(string literal)
    {
        if (literal.Contains("WITH RECURSIVE", StringComparison.OrdinalIgnoreCase)) return SqlSegmentPreprocessor.SafeReplaceKeyword(literal, "WITH RECURSIVE", "WITH");
        return literal;
    }

    protected override bool TryRewriteLock(SqlLockFragment lockFrag, IReadOnlyList<SqlSegment> segments, List<SqlSegment> rewritten, ref int i)
    {
        int lastTableReferenceIndex = rewritten.FindLastIndex(s => s.Type == SqlSegmentType.Reference && s.Value is ISqlEntityBase);

        if (lastTableReferenceIndex > -1)
        {
            string hint = lockFrag.Mode switch
            {
                SqlLockMode.Update => " WITH (UPDLOCK)",
                SqlLockMode.Share => " WITH (ROWLOCK, HOLDLOCK)",
                SqlLockMode.NoLock => " WITH (NOLOCK)",
                _ => ""
            };

            if (!string.IsNullOrEmpty(hint))
            {
                for (int k = rewritten.Count - 1; k > lastTableReferenceIndex; k--)
                {
                    if (rewritten[k].Type == SqlSegmentType.Literal && string.IsNullOrWhiteSpace(rewritten[k].Value as string)) rewritten.RemoveAt(k);
                }
                rewritten.Insert(lastTableReferenceIndex + 1, new SqlSegment(SqlSegmentType.Literal, hint));
            }
            return true;
        }
        return false;
    }

    protected override bool TryRewriteUpsert(SqlSegment segment, IReadOnlyList<SqlSegment> segments, List<SqlSegment> rewritten, ref int i)
    {
        bool isOnConflict = segment.HasTag(SqlSegmentTag.OnConflictKeyword) || 
            (segment.Type == SqlSegmentType.Literal && segment.Value is string s1 && SqlRewriterHelpers.ContainsKeyword(s1, SqlKeyword.OnConflict.Value));

        if (!isOnConflict) return false;

        ISqlEntityBase? targetTable = null;
        SqlInsertValuesFragment? insertFrag = null;
        int tableIndex = -1;

        for (int j = rewritten.Count - 1; j >= 0; j--)
        {
            if (rewritten[j].Value is SqlInsertValuesFragment ins && insertFrag == null) insertFrag = ins;
            else if (rewritten[j].Value is ISqlEntityBase t && targetTable == null) { targetTable = t; tableIndex = j; }
        }

        var conflictCols = new List<ISqlProjection>();
        SqlSetFragment? setFrag = null;
        int lookahead = 1;

        while (i + lookahead < segments.Count)
        {
            var next = segments[i + lookahead];
            if (next.Value is SqlColumnReferenceBase colRefBase) conflictCols.Add(new SqlUnqualifiedColumn(colRefBase.ColumnName));
            else if (next.Value is ISqlProjection p) conflictCols.Add(p);
            else if (next.Value is SqlSetFragment sf) { setFrag = sf; lookahead++; break; }
            lookahead++;
        }

        if (tableIndex > -1 && targetTable != null && insertFrag != null && conflictCols.Count > 0 && setFrag != null)
        {
            int insertKeywordIndex = tableIndex > 0 ? tableIndex - 1 : 0;
            for (int k = tableIndex; k >= 0; k--)
            {
                if (rewritten[k].Type == SqlSegmentType.Literal && (rewritten[k].Value as string)?.IndexOf(SqlKeyword.Insert.Value, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    insertKeywordIndex = k;
                    break;
                }
            }

            rewritten.RemoveRange(insertKeywordIndex, rewritten.Count - insertKeywordIndex);
            rewritten.Add(new SqlSegment(SqlSegmentType.Raw, new SqlServerMergeFragment(targetTable, insertFrag, conflictCols, setFrag)));
            i += (lookahead - 1); 
            return true;
        }
        return false;
    }

    protected override bool TryRewriteReturning(SqlSegment segment, IReadOnlyList<SqlSegment> segments, List<SqlSegment> rewritten, ref int i)
    {
        if (!segment.HasTag(SqlSegmentTag.ReturningKeyword)) return false;

        var projections = new List<ISqlProjection>();
        int lookaheadOffset = 1;

        while (i + lookaheadOffset < segments.Count)
        {
            var nextSeg = segments[i + lookaheadOffset];
            if (nextSeg.Value is ISqlProjection proj) { projections.Add(proj); lookaheadOffset++; }
            else if (nextSeg.Type == SqlSegmentType.Literal && nextSeg.Value is string s && string.IsNullOrWhiteSpace(s.Replace(",", ""))) lookaheadOffset++;
            else break; 
        }

        if (projections.Count > 0)
        {
            for (int j = rewritten.Count - 1; j >= 0; j--)
            {
                if (rewritten[j].Value is SqlInsertValuesFragment insertFrag)
                {
                    rewritten[j] = new SqlSegment(SqlSegmentType.Raw, new SqlServerInsertValuesFragment(insertFrag, projections));
                    
                    for (int k = rewritten.Count - 1; k > j; k--)
                    {
                        if (rewritten[k].Type == SqlSegmentType.Literal && string.IsNullOrWhiteSpace(rewritten[k].Value as string)) rewritten.RemoveAt(k);
                        else break;
                    }

                    if (segment.Value is string text)
                    {
                        int index = text.LastIndexOf(SqlKeyword.Returning.Value, StringComparison.OrdinalIgnoreCase);
                        if (index > 0) 
                        {
                            var precedingText = text[..index].TrimEnd();
                            if (precedingText.Length > 0) rewritten.Add(new SqlSegment(SqlSegmentType.Literal, precedingText));
                        }
                    }
                    i += (lookaheadOffset - 1); 
                    return true;
                }
            }
        }
        return false;
    }

    protected override bool TryRewritePaging(SqlSegment segment, IReadOnlyList<SqlSegment> segments, List<SqlSegment> rewritten, ref int i)
    {
        if (!segment.HasTag(SqlSegmentTag.Paging) || !SqlRewriterHelpers.TryExtractPagingParameters(segments, i, out var limitParam, out var offsetParam, out int nextIndex)) return false;

        if (segment.Value is string textPaging)
        {
            int index = textPaging.LastIndexOf(SqlKeyword.Limit.Value, StringComparison.OrdinalIgnoreCase);
            if (index > -1) rewritten.Add(new SqlSegment(SqlSegmentType.Literal, textPaging[..index]));
        }

        rewritten.Add(new SqlSegment(SqlSegmentType.Literal, $"{SqlKeyword.Offset.Value} "));
        rewritten.Add(offsetParam); 
        rewritten.Add(new SqlSegment(SqlSegmentType.Literal, " ROWS FETCH NEXT "));
        rewritten.Add(limitParam); 
        rewritten.Add(new SqlSegment(SqlSegmentType.Literal, " ROWS ONLY"));

        i = nextIndex;
        return true;
    }

    protected override SqlMultiTableUpdateFragment? CreateMultiTableUpdate(SqlUpdateAsFragment upAsFrag, SqlSetFragment setFrag, List<SqlSegment> rewritten, int whereKeywordIdx, ISqlContext context)
    {
        var targetEntity = upAsFrag.Target;
        string quotedAlias = context.Dialect.QuoteIdentifier(targetEntity.Reference.Alias ?? "tgt");
        var targetFrag = new SqlRawFragment(quotedAlias);
        
        var fromFrag = new SqlSegmentCollectionFragment([
            new SqlSegment(SqlSegmentType.Reference, targetEntity, SqlRenderMode.BaseName),
            new SqlSegment(SqlSegmentType.Literal, " AS "),
            new SqlSegment(SqlSegmentType.Raw, targetFrag)
        ]);

        SqlSegmentCollectionFragment? whereClause = null;
        if (whereKeywordIdx > -1) whereClause = new SqlSegmentCollectionFragment(rewritten.Skip(whereKeywordIdx + 1).ToList());

        return new SqlMultiTableUpdateFragment(targetFrag, setFrag, fromFrag, whereClause);
    }
}