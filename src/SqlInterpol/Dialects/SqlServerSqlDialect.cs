using SqlInterpol.Config;
using SqlInterpol.Parsing;

namespace SqlInterpol.Dialects;

public class SqlServerSqlDialect : SqlDialectBase
{
    public override SqlDialectKind Kind => SqlDialectKind.SqlServer;
    public override string OpenQuote => "[";
    public override string CloseQuote => "]";
    public override string ParameterPrefix => "@p";

    public override string RenderFragment(ISqlFragment fragment, ISqlContext context)
    {
        if (fragment is SqlPagingFragment p)
        {
            return $"OFFSET {p.Offset} ROWS FETCH NEXT {p.Limit} ROWS ONLY";
        }

        return base.RenderFragment(fragment, context);
    }

    public override IEnumerable<SqlSegment> RewriteSegments(IReadOnlyList<SqlSegment> segments)
    {
        var baseRewritten = base.RewriteSegments(segments).ToList();
        var rewritten = new List<SqlSegment>(baseRewritten.Count);

        for (int i = 0; i < baseRewritten.Count; i++)
        {
            var segment = baseRewritten[i];

            // 1. Transpile RETURNING -> OUTPUT (Now supports multiple columns!)
            if (segment.Tag == SqlSegmentTag.ReturningKeyword)
            {
                var projections = new List<ISqlProjection>();
                int lookaheadOffset = 1;

                // Look ahead to gather all projections and commas
                while (i + lookaheadOffset < baseRewritten.Count)
                {
                    var nextSeg = baseRewritten[i + lookaheadOffset];
                    
                    if (nextSeg.Value is ISqlProjection proj)
                    {
                        projections.Add(proj);
                        lookaheadOffset++;
                    }
                    // If it's a literal containing ONLY whitespace or commas, swallow it and keep looking
                    else if (nextSeg.Type == SqlSegmentType.Literal && nextSeg.Value is string s && string.IsNullOrWhiteSpace(s.Replace(",", "")))
                    {
                        lookaheadOffset++;
                    }
                    else
                    {
                        break; // We hit the end of the returning list
                    }
                }

                if (projections.Count > 0)
                {
                    // Look backwards in our rewritten list to find the DTO Insert Fragment
                    for (int j = rewritten.Count - 1; j >= 0; j--)
                    {
                        if (rewritten[j].Value is SqlInsertValuesFragment insertFrag)
                        {
                            // Upgrade the DTO fragment and pass the LIST of projections!
                            rewritten[j] = new SqlSegment(SqlSegmentType.Raw, new SqlServerInsertValuesFragment(insertFrag, projections));
                            
                            // Preserve any formatting that came BEFORE the word RETURNING
                            if (segment.Value is string text)
                            {
                                int index = text.LastIndexOf(SqlKeyword.Returning, StringComparison.OrdinalIgnoreCase);
                                if (index > 0)
                                {
                                    rewritten.Add(new SqlSegment(SqlSegmentType.Literal, text[..index]));
                                }
                            }

                            // Advance 'i' past the RETURNING keyword and ALL the collected projections/commas
                            i += (lookaheadOffset - 1); 
                            goto NextSegment; 
                        }
                    }
                }
            }

            // 2. Apply SQL Server specific paging logic
            if (segment.Tag == SqlSegmentTag.Paging && segment.Value is string textPaging)
            {
                if (i + 3 < baseRewritten.Count &&
                    baseRewritten[i + 1].Type == SqlSegmentType.Parameter &&
                    baseRewritten[i + 3].Type == SqlSegmentType.Parameter)
                {
                    int index = textPaging.LastIndexOf(SqlKeyword.Limit, StringComparison.OrdinalIgnoreCase);
                    
                    if (index > -1)
                    {
                        rewritten.Add(new SqlSegment(SqlSegmentType.Literal, textPaging[..index]));
                    }

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

// 3. Update the Wrapper to handle a List of Projections
public class SqlServerInsertValuesFragment(SqlInsertValuesFragment original, IReadOnlyList<ISqlProjection> returnedColumns) : ISqlFragment, ISqlParameterGenerator
{
    public void GenerateParameters(ISqlContext context) => original.GenerateParameters(context);
    
    public string ToSql(ISqlContext context, SqlRenderMode mode = SqlRenderMode.Default)
    {
        var baseSql = original.ToSql(context, mode);
        
        // Map all columns to inserted.[Col] and join with commas
        var outputCols = string.Join(", ", returnedColumns.Select(c => $"inserted.{c.ToSql(context, SqlRenderMode.BaseName)}"));
        
        return baseSql.Replace(SqlKeyword.Values, $"OUTPUT {outputCols}{Environment.NewLine}{SqlKeyword.Values}");
    }
}