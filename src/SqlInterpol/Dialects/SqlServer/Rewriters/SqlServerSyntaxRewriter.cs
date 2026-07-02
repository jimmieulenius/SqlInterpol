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

                // =====================================================================
                // FIX: ROBUST BACKWARD AST HUNTING
                // Ignore all whitespace/literals and just grab the physical fragments
                // =====================================================================
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

                // =====================================================================
                // FIX: ROBUST FORWARD AST HUNTING
                // Scan forward until we hit the SET fragment, unwrapping any columns we find!
                // =====================================================================
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
                    // Find the exact location of the INSERT keyword
                    int insertKeywordIndex = tableIndex > 0 ? tableIndex - 1 : 0;
                    for (int k = tableIndex; k >= 0; k--)
                    {
                        if (rewritten[k].Type == SqlSegmentType.Literal && (rewritten[k].Value as string)?.IndexOf("INSERT", StringComparison.OrdinalIgnoreCase) >= 0)
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

            // =========================================================================
            // Relocate and Transpile SqlLockFragment to SQL Server table HINTS
            // =========================================================================
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
                                int index = text.LastIndexOf("RETURNING", StringComparison.OrdinalIgnoreCase);
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

            if (segment.HasTag(SqlSegmentTag.Paging) && segment.Value is string textPaging)
            {
                int p1Idx = i + 1;
                while (p1Idx < segments.Count && segments[p1Idx].Type == SqlSegmentType.Literal && string.IsNullOrWhiteSpace(segments[p1Idx].Value as string)) p1Idx++;
                
                int p2Idx = p1Idx + 1;
                while (p2Idx < segments.Count && segments[p2Idx].Type == SqlSegmentType.Literal && string.IsNullOrWhiteSpace(segments[p2Idx].Value as string)) p2Idx++;
                
                int p3Idx = p2Idx + 1;
                while (p3Idx < segments.Count && segments[p3Idx].Type == SqlSegmentType.Literal && string.IsNullOrWhiteSpace(segments[p3Idx].Value as string)) p3Idx++;

                if (p3Idx < segments.Count && segments[p1Idx].Type == SqlSegmentType.Parameter && segments[p3Idx].Type == SqlSegmentType.Parameter)
                {
                    int index = textPaging.LastIndexOf(SqlKeyword.Limit, StringComparison.OrdinalIgnoreCase);
                    if (index > -1) rewritten.Add(new SqlSegment(SqlSegmentType.Literal, textPaging[..index]));

                    rewritten.Add(new SqlSegment(SqlSegmentType.Literal, $"{SqlKeyword.Offset} "));
                    rewritten.Add(segments[p3Idx]); 
                    
                    rewritten.Add(new SqlSegment(SqlSegmentType.Literal, " ROWS FETCH NEXT "));
                    rewritten.Add(segments[p1Idx]); 
                    
                    rewritten.Add(new SqlSegment(SqlSegmentType.Literal, " ROWS ONLY"));

                    i = p3Idx;
                    continue;
                }
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

    // =========================================================================
    // HELPER: UNQUALIFIED COLUMN PROXY
    // =========================================================================
    /// <summary>
    /// A lightweight proxy that strips the table entity from a projection 
    /// so it renders as an unqualified column name (perfect for MERGE statements).
    /// </summary>
    private class SqlUnqualifiedColumn : ISqlProjection
    {
        private readonly string _columnName;

        // Dummy implementation to satisfy interface; the renderer never reads it for base name rendering
        public ISqlReference Reference => null!; 
        
        public string PropertyName => _columnName;
        
        public SqlUnqualifiedColumn(string columnName)
        {
            _columnName = columnName;
        }

        public string ToSql(ISqlContext context, SqlRenderMode mode = SqlRenderMode.Default)
        {
            return context.Dialect.QuoteIdentifier(_columnName);
        }
    }
}