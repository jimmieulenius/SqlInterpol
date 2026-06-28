using SqlInterpol.Parsing;

namespace SqlInterpol.Dialects.SqlServer;

/// <summary>
/// A structural rewriter that transpiles generic Upserts into MERGE statements, maps UPDATE AS fragments, 
/// and handles SQL Server's custom OUTPUT inserted.* logic.
/// </summary>
public class SqlServerSyntaxRewriter : ISqlSegmentRewriter
{
    /// <inheritdoc />
    public bool IsApplicable(ISqlCompilationState state) => true;

    /// <inheritdoc />
    public IReadOnlyList<SqlSegment> Rewrite(IReadOnlyList<SqlSegment> segments, ISqlContext context)
    {
        var rewritten = new List<SqlSegment>(segments.Count);

        for (int i = 0; i < segments.Count; i++)
        {
            var segment = segments[i];

            bool isOnConflict = segment.HasTag(SqlSegmentTag.OnConflictKeyword) || 
                (segment.Type == SqlSegmentType.Literal && segment.Value is string s1 && s1.Contains("ON CONFLICT", StringComparison.OrdinalIgnoreCase));

            if (segment.Type == SqlSegmentType.Literal && segment.Value is string literalValue &&
                literalValue.Contains("WITH RECURSIVE", StringComparison.OrdinalIgnoreCase))
            {
                segment = new SqlSegment(
                    SqlSegmentType.Literal, 
                    literalValue.Replace("WITH RECURSIVE", "WITH", StringComparison.OrdinalIgnoreCase), 
                    segment.RenderMode, 
                    segment.Tags);
            }

            if (isOnConflict)
            {
                ISqlEntityBase? targetTable = null;
                SqlInsertValuesFragment? insertFrag = null;
                int tableIndex = -1;

                for (int j = rewritten.Count - 1; j >= 0; j--)
                {
                    if (rewritten[j].Value is SqlInsertValuesFragment ins) insertFrag = ins;
                    else if (rewritten[j].Value is ISqlEntityBase t && insertFrag != null) { targetTable = t; tableIndex = j; break; }
                }

                var conflictCols = new List<ISqlProjection>();
                SqlSetFragment? setFrag = null;
                int lookahead = 1;

                while (i + lookahead < segments.Count)
                {
                    var next = segments[i + lookahead];
                    
                    bool isDoUpdate = next.HasTag(SqlSegmentTag.DoUpdateSetKeyword) || 
                                      (next.Type == SqlSegmentType.Literal && next.Value is string s2 && s2.Contains("DO UPDATE", StringComparison.OrdinalIgnoreCase));

                    if (next.Value is ISqlProjection p) conflictCols.Add(p);
                    else if (isDoUpdate)
                    {
                        int setLookahead = 1;
                        while (i + lookahead + setLookahead < segments.Count && 
                               segments[i + lookahead + setLookahead].Type == SqlSegmentType.Literal && 
                               string.IsNullOrWhiteSpace(segments[i + lookahead + setLookahead].Value as string))
                        {
                            setLookahead++;
                        }

                        if (i + lookahead + setLookahead < segments.Count && segments[i + lookahead + setLookahead].Value is SqlSetFragment sf)
                        {
                            setFrag = sf;
                            lookahead += setLookahead;
                        }
                        break;
                    }
                    lookahead++;
                }

                if (tableIndex > -1 && targetTable != null && insertFrag != null && conflictCols.Count > 0 && setFrag != null)
                {
                    int insertKeywordIndex = tableIndex > 0 ? tableIndex - 1 : 0;
                    rewritten.RemoveRange(insertKeywordIndex, rewritten.Count - insertKeywordIndex);
                    rewritten.Add(new SqlSegment(SqlSegmentType.Raw, new SqlServerMergeFragment(targetTable, insertFrag, conflictCols, setFrag)));

                    i += lookahead; 
                    continue;
                }
            }

            if (segment.HasTag(SqlSegmentTag.ReturningKeyword))
            {
                var projections = new List<ISqlProjection>();
                int lookaheadOffset = 1;

                while (i + lookaheadOffset < segments.Count)
                {
                    var nextSeg = segments[i + lookaheadOffset];
                    
                    if (nextSeg.Value is ISqlProjection proj)
                    {
                        projections.Add(proj);
                        lookaheadOffset++;
                    }
                    else if (nextSeg.Type == SqlSegmentType.Literal && nextSeg.Value is string s && string.IsNullOrWhiteSpace(s.Replace(",", "")))
                    {
                        lookaheadOffset++;
                    }
                    else break; 
                }

                if (projections.Count > 0)
                {
                    for (int j = rewritten.Count - 1; j >= 0; j--)
                    {
                        if (rewritten[j].Value is SqlInsertValuesFragment insertFrag)
                        {
                            rewritten[j] = new SqlSegment(SqlSegmentType.Raw, new SqlServerInsertValuesFragment(insertFrag, projections));
                            
                            if (segment.Value is string text)
                            {
                                int index = text.LastIndexOf("RETURNING", StringComparison.OrdinalIgnoreCase);
                                if (index > 0) rewritten.Add(new SqlSegment(SqlSegmentType.Literal, text[..index].TrimEnd()));
                            }
                            i += (lookaheadOffset - 1); 
                            goto NextSegment; 
                        }
                    }
                }
            }

            if (segment.HasTag(SqlSegmentTag.Paging) && segment.Value is string textPaging)
            {
                if (i + 3 < segments.Count && segments[i + 1].Type == SqlSegmentType.Parameter && segments[i + 3].Type == SqlSegmentType.Parameter)
                {
                    int index = textPaging.LastIndexOf(SqlKeyword.Limit, StringComparison.OrdinalIgnoreCase);
                    if (index > -1) rewritten.Add(new SqlSegment(SqlSegmentType.Literal, textPaging[..index]));

                    rewritten.Add(new SqlSegment(SqlSegmentType.Literal, $"{SqlKeyword.Offset} "));
                    rewritten.Add(segments[i + 3]); 
                    
                    rewritten.Add(new SqlSegment(SqlSegmentType.Literal, " ROWS FETCH NEXT "));
                    rewritten.Add(segments[i + 1]); 
                    
                    rewritten.Add(new SqlSegment(SqlSegmentType.Literal, " ROWS ONLY"));

                    i += 3;
                    continue;
                }
            }

            rewritten.Add(segment);
            NextSegment: continue;
        }

        int upAsIdx = rewritten.FindIndex(s => s.Type == SqlSegmentType.Raw && s.Value is SqlUpdateAsFragment);
        
        // FIX: Ensure we do NOT apply the Update AS transformation if this query has already been elevated to a CTE or Subquery Update by the core engine!
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
                
                string quotedAlias = context.Dialect.QuoteIdentifier(targetEntity.Reference.Alias ?? "tgt");
                var targetFrag = new SqlRawFragment(quotedAlias);
                
                var fromFrag = new SqlSegmentCollectionFragment([
                    new SqlSegment(SqlSegmentType.Reference, targetEntity, SqlRenderMode.BaseName),
                    new SqlSegment(SqlSegmentType.Literal, " AS "),
                    new SqlSegment(SqlSegmentType.Raw, targetFrag)
                ]);

                SqlSegmentCollectionFragment? whereClause = null;
                if (whereKeywordIdx > -1)
                {
                    whereClause = new SqlSegmentCollectionFragment(rewritten.Skip(whereKeywordIdx + 1).ToList());
                }

                var multiTableUpdate = new SqlMultiTableUpdateFragment(targetFrag, setFrag, fromFrag, whereClause);
                
                rewritten.RemoveRange(upAsIdx, rewritten.Count - upAsIdx);
                rewritten.Add(new SqlSegment(SqlSegmentType.Raw, multiTableUpdate));
            }
        }

        return rewritten;
    }
}