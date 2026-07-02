using SqlInterpol.Parsing;

namespace SqlInterpol.Dialects.SqlServer;

/// <summary>
/// A structural rewriter that transpiles generic Upserts into MERGE statements, maps UPDATE AS fragments, 
/// handles SQL Server's custom OUTPUT inserted.* logic, transposing FOR UPDATE/SHARE locking hints, 
/// and transpiles LIMIT/OFFSET paging.
/// </summary>
public class SqlServerSyntaxRewriter : ISqlSegmentRewriter
{
    /// <inheritdoc />
    public bool IsApplicable(ISqlCompilationState state) => true;

    /// <inheritdoc />
    public IReadOnlyList<SqlSegment> Rewrite(IReadOnlyList<SqlSegment> segments, ISqlContext context)
    {
        var rewritten = new List<SqlSegment>(segments.Count);
        int lastTableReferenceIndex = -1;

        for (int i = 0; i < segments.Count; i++)
        {
            var segment = segments[i];

            if (segment.Type == SqlSegmentType.Reference && segment.Value is ISqlEntityBase)
            {
                lastTableReferenceIndex = rewritten.Count;
            }

            bool isOnConflict = segment.HasTag(SqlSegmentTag.OnConflictKeyword) || 
                (segment.Type == SqlSegmentType.Literal && segment.Value is string s1 && SqlRewriterHelpers.ContainsKeyword(s1, SqlKeyword.OnConflict.Value));

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
                    if (rewritten[j].Value is SqlInsertValuesFragment ins && insertFrag == null) 
                        insertFrag = ins;
                    else if (rewritten[j].Value is ISqlEntityBase t && targetTable == null) 
                    { 
                        targetTable = t; 
                        tableIndex = j; 
                    }
                }

                var conflictCols = new List<ISqlProjection>();
                SqlSetFragment? setFrag = null;
                int lookahead = 1;

                while (i + lookahead < segments.Count)
                {
                    var next = segments[i + lookahead];
                    
                    if (next.Value is SqlColumnReferenceBase colRefBase)
                    {
                        conflictCols.Add(new SqlUnqualifiedColumn(colRefBase.ColumnName));
                    }
                    else if (next.Value is ISqlProjection p)
                    {
                        conflictCols.Add(p);
                    }
                    else if (next.Value is SqlSetFragment sf)
                    {
                        setFrag = sf;
                        lookahead++;
                        break;
                    }
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
                    continue;
                }
            }

            if (segment.Type == SqlSegmentType.Raw && segment.Value is SqlLockFragment lockFrag)
            {
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
                            if (rewritten[k].Type == SqlSegmentType.Literal && string.IsNullOrWhiteSpace(rewritten[k].Value as string))
                            {
                                rewritten.RemoveAt(k);
                            }
                        }

                        rewritten.Insert(lastTableReferenceIndex + 1, new SqlSegment(SqlSegmentType.Literal, hint));
                        lastTableReferenceIndex = -1; 
                    }
                }
                continue; 
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
                            
                            for (int k = rewritten.Count - 1; k > j; k--)
                            {
                                if (rewritten[k].Type == SqlSegmentType.Literal && string.IsNullOrWhiteSpace(rewritten[k].Value as string))
                                {
                                    rewritten.RemoveAt(k);
                                }
                                else
                                {
                                    break;
                                }
                            }

                            if (segment.Value is string text)
                            {
                                int index = text.LastIndexOf(SqlKeyword.Returning.Value, StringComparison.OrdinalIgnoreCase);
                                if (index > 0) 
                                {
                                    var precedingText = text[..index].TrimEnd();
                                    if (precedingText.Length > 0)
                                    {
                                        rewritten.Add(new SqlSegment(SqlSegmentType.Literal, precedingText));
                                    }
                                }
                            }
                            i += (lookaheadOffset - 1); 
                            goto NextSegment; 
                        }
                    }
                }
            }

            if (segment.HasTag(SqlSegmentTag.Paging) && SqlRewriterHelpers.TryExtractPagingParameters(segments, i, out var limitParam, out var offsetParam, out int nextIndex))
            {
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
                continue;
            }

            rewritten.Add(segment);
            NextSegment: continue;
        }

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

    private class SqlUnqualifiedColumn : ISqlProjection
    {
        private readonly string _columnName;
        public ISqlReference Reference => null!; 
        public string PropertyName => _columnName;
        public SqlUnqualifiedColumn(string columnName) => _columnName = columnName;
        public string ToSql(ISqlContext context, SqlRenderMode mode = SqlRenderMode.Default) => context.Dialect.QuoteIdentifier(_columnName);
    }
}