using SqlInterpol.Config;
using SqlInterpol.Parsing;

namespace SqlInterpol.Dialects;

public class SqlServerSqlDialect : SqlDialectBase
{
    public override SqlDialectKind Kind => SqlDialectKind.SqlServer;
    public override string OpenQuote => "[";
    public override string CloseQuote => "]";
    public override string ParameterPrefix => "@p";
    public override IReadOnlySet<SqlFeature> SupportedFeatures { get; } = new HashSet<SqlFeature>
    {
        SqlFeature.ForUpdate, // Emulated via WITH (UPDLOCK)
        SqlFeature.ForShare,
        SqlFeature.Returning, // Emulated via OUTPUT inserted.*
        SqlFeature.OnConflict // Emulated via MERGE
    };

    public override string RenderFragment(ISqlFragment fragment, ISqlContext context)
    {
        return fragment switch
        {
            // The leading space here is intentional! 
            // It perfectly replaces the space that .TrimEnd(' ', '\t') removed in SqlDialectBase.
            SqlLockFragment { Mode: SqlLockMode.Update } => " WITH (UPDLOCK)",
            SqlLockFragment { Mode: SqlLockMode.Share }  => " WITH (ROWLOCK, HOLDLOCK)",
            SqlLockFragment { Mode: SqlLockMode.NoLock } => " WITH (NOLOCK)",
            
            // Handle SQL Server specific pagination (OFFSET / FETCH)
            SqlPagingFragment p => $"OFFSET {p.Offset} ROWS FETCH NEXT {p.Limit} ROWS ONLY",
            
            // Pass everything else down to the base dialect
            _ => base.RenderFragment(fragment, context)
        };
    }

    public override IEnumerable<SqlSegment> RewriteSegments(IReadOnlyList<SqlSegment> segments)
    {
        var baseRewritten = base.RewriteSegments(segments).ToList();
        var rewritten = new List<SqlSegment>(baseRewritten.Count);

        for (int i = 0; i < baseRewritten.Count; i++)
        {
            var segment = baseRewritten[i];

            bool isOnConflict = segment.Tag == SqlSegmentTag.OnConflictKeyword || 
                (segment.Type == SqlSegmentType.Literal && segment.Value is string s1 && s1.Contains("ON CONFLICT", StringComparison.OrdinalIgnoreCase));

            // 0. Strip RECURSIVE keyword (SQL Server CTEs are implicitly recursive)
            if (segment.Type == SqlSegmentType.Literal && segment.Value is string literalValue &&
                literalValue.Contains("WITH RECURSIVE", StringComparison.OrdinalIgnoreCase))
            {
                segment = new SqlSegment(SqlSegmentType.Literal,
                    SqlInterpolationParser.Instance.ReplaceKeyword(literalValue, "WITH RECURSIVE", "WITH"));
            }

            // 1. Transpile UPSERT -> ANSI MERGE
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

                while (i + lookahead < baseRewritten.Count)
                {
                    var next = baseRewritten[i + lookahead];
                    
                    bool isDoUpdate = next.Tag == SqlSegmentTag.DoUpdateSetKeyword || 
                                     (next.Type == SqlSegmentType.Literal && next.Value is string s2 && s2.Contains("DO UPDATE", StringComparison.OrdinalIgnoreCase));

                    if (next.Value is ISqlProjection p) conflictCols.Add(p);
                    else if (isDoUpdate)
                    {
                        if (i + lookahead + 1 < baseRewritten.Count && baseRewritten[i + lookahead + 1].Value is SqlSetFragment sf)
                        {
                            setFrag = sf;
                            lookahead++;
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
                    goto NextSegment;
                }
            }

            // 2. Transpile RETURNING -> OUTPUT 
            if (segment.Tag == SqlSegmentTag.ReturningKeyword)
            {
                var projections = new List<ISqlProjection>();
                int lookaheadOffset = 1;

                while (i + lookaheadOffset < baseRewritten.Count)
                {
                    var nextSeg = baseRewritten[i + lookaheadOffset];
                    
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

            // 3. Apply SQL Server specific paging logic
            if (segment.Tag == SqlSegmentTag.Paging && segment.Value is string textPaging)
            {
                if (i + 3 < baseRewritten.Count && baseRewritten[i + 1].Type == SqlSegmentType.Parameter && baseRewritten[i + 3].Type == SqlSegmentType.Parameter)
                {
                    int index = textPaging.LastIndexOf(SqlKeyword.Limit, StringComparison.OrdinalIgnoreCase);
                    if (index > -1) rewritten.Add(new SqlSegment(SqlSegmentType.Literal, textPaging[..index]));

                    rewritten.Add(new SqlSegment(SqlSegmentType.Literal, $"{SqlKeyword.Offset} "));
                    rewritten.Add(baseRewritten[i + 3]); // offset param
                    
                    rewritten.Add(new SqlSegment(SqlSegmentType.Literal, " ROWS FETCH NEXT "));
                    rewritten.Add(baseRewritten[i + 1]); // limit param
                    
                    rewritten.Add(new SqlSegment(SqlSegmentType.Literal, " ROWS ONLY"));

                    i += 3;
                    continue;
                }
            }

            rewritten.Add(segment);
            NextSegment: continue;
        }

        return rewritten;
    }
}

public class SqlServerInsertValuesFragment(SqlInsertValuesFragment original, IReadOnlyList<ISqlProjection> returnedColumns) : ISqlFragment, ISqlParameterGenerator
{
    public void GenerateParameters(ISqlContext context) => original.GenerateParameters(context);
    public string ToSql(ISqlContext context, SqlRenderMode mode = SqlRenderMode.Default)
    {
        var baseSql = original.ToSql(context, mode);
        var outputCols = string.Join(", ", returnedColumns.Select(c => $"inserted.{c.ToSql(context, SqlRenderMode.BaseName)}"));
        return baseSql.Replace(SqlKeyword.Values, $"OUTPUT {outputCols}{Environment.NewLine}{SqlKeyword.Values}");
    }
}

public class SqlServerMergeFragment(
    ISqlEntityBase targetTable,
    SqlInsertValuesFragment insertFragment,
    IReadOnlyList<ISqlProjection> conflictColumns,
    SqlSetFragment updateFragment) : ISqlFragment
{
    public string ToSql(ISqlContext context, SqlRenderMode mode = SqlRenderMode.Default)
    {
        var target = targetTable.ToSql(context);
        var insertCols = insertFragment.Assignments[0].Select(a => a.Reference.ToSql(context, SqlRenderMode.BaseName)).ToList();
        
        var sourceRows = new List<string>();

        foreach (var row in insertFragment.Assignments)
        {
            var vals = row.Select(a => a.ToSql(context).Split('=').Last().Trim());
            sourceRows.Add($"({string.Join(", ", vals)})");
        }
        
        var usingClause = $"USING (VALUES {string.Join(", ", sourceRows)}) AS source({string.Join(", ", insertCols)})";
        var onClause = string.Join(" AND ", conflictColumns.Select(c => $"target.{c.ToSql(context, SqlRenderMode.BaseName)} = source.{c.ToSql(context, SqlRenderMode.BaseName)}"));
        var updateSets = updateFragment.Assignments.Select(a => $"target.{a.Reference.ToSql(context, SqlRenderMode.BaseName)} = {a.ToSql(context).Split('=').Last().Trim()}");
        var insertVals = insertFragment.Assignments[0].Select(a => $"source.{a.Reference.ToSql(context, SqlRenderMode.BaseName)}");

        var nl = Environment.NewLine;
        return $"MERGE INTO {target} AS target{nl}{usingClause}{nl}ON {onClause}{nl}WHEN MATCHED THEN{nl}  UPDATE SET {string.Join(", ", updateSets)}{nl}WHEN NOT MATCHED THEN{nl}  INSERT ({string.Join(", ", insertCols)}){nl}  VALUES ({string.Join(", ", insertVals)});";
    }
}