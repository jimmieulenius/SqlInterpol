using SqlInterpol.Config;
using SqlInterpol.Parsing;

namespace SqlInterpol.Dialects;

public class MySqlSqlDialect : SqlDialectBase
{
    public override SqlDialectKind Kind => SqlDialectKind.MySql;
    public override string OpenQuote => "`";
    public override string CloseQuote => "`";
    public override string ParameterPrefix => "@p";

    public override IEnumerable<SqlSegment> RewriteSegments(IReadOnlyList<SqlSegment> segments)
    {
        var baseRewritten = base.RewriteSegments(segments).ToList();
        var rewritten = new List<SqlSegment>(baseRewritten.Count);

        for (int i = 0; i < baseRewritten.Count; i++)
        {
            var segment = baseRewritten[i];

            bool isOnConflict = segment.Tag == SqlSegmentTag.OnConflictKeyword || 
                               (segment.Type == SqlSegmentType.Literal && segment.Value is string s1 && s1.Contains("ON CONFLICT", StringComparison.OrdinalIgnoreCase));

            if (isOnConflict)
            {
                int doUpdateIdx = -1;
                for (int j = i + 1; j < baseRewritten.Count; j++)
                {
                    var next = baseRewritten[j];
                    bool isDoUpdate = next.Tag == SqlSegmentTag.DoUpdateSetKeyword || 
                                     (next.Type == SqlSegmentType.Literal && next.Value is string s2 && s2.Contains("DO UPDATE", StringComparison.OrdinalIgnoreCase));
                    
                    if (isDoUpdate) { doUpdateIdx = j; break; }
                }

                if (doUpdateIdx > -1)
                {
                    if (segment.Value is string text)
                    {
                        int idx = text.LastIndexOf("ON CONFLICT", StringComparison.OrdinalIgnoreCase);
                        
                        // FIX: Preserve formatting exactly like VALUES and SET!
                        if (idx > -1) 
                        {
                            rewritten.Add(new SqlSegment(SqlSegmentType.Literal, text[..idx]));
                        }
                    }

                    // FIX: Removed the hardcoded \n so it relies purely on the preserved formatting
                    rewritten.Add(new SqlSegment(SqlSegmentType.Literal, "ON DUPLICATE KEY UPDATE"));

                    i = doUpdateIdx;
                    
                    if (i + 1 < baseRewritten.Count && baseRewritten[i + 1].Value is SqlSetFragment setFrag)
                    {
                        rewritten.Add(new SqlSegment(SqlSegmentType.Raw, new MySqlUpdateFragment(setFrag)));
                        i++; 
                    }
                    continue;
                }
            }
            rewritten.Add(segment);
        }
        return rewritten;
    }
}

public class MySqlUpdateFragment(SqlSetFragment original) : ISqlFragment
{
    public string ToSql(ISqlContext context, SqlRenderMode mode = SqlRenderMode.Default)
    {
        var sql = original.ToSql(context, mode);
        if (sql.StartsWith("SET", StringComparison.OrdinalIgnoreCase)) return " " + sql[3..].TrimStart();
        return " " + sql;
    }
}