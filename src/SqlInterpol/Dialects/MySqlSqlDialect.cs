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
        
        SqlLockMode? deferredLock = null;

        for (int i = 0; i < baseRewritten.Count; i++)
        {
            var segment = baseRewritten[i];

            // 1. Extract and swallow Lock Fragments
            if (segment.Type == SqlSegmentType.Raw && segment.Value is SqlLockFragment lockFrag)
            {
                deferredLock = lockFrag.Mode;
                continue; // Do not add it to the rewritten stream (erases it inline)
            }

            // 2. Existing ON CONFLICT -> ON DUPLICATE KEY UPDATE logic
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
                        if (idx > -1) 
                        {
                            rewritten.Add(new SqlSegment(SqlSegmentType.Literal, text[..idx]));
                        }
                    }

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

        // 3. Append deferred locks to the end of the query
        if (deferredLock == SqlLockMode.Update)
            rewritten.Add(new SqlSegment(SqlSegmentType.Literal, "\nFOR UPDATE"));
        else if (deferredLock == SqlLockMode.Share)
            rewritten.Add(new SqlSegment(SqlSegmentType.Literal, "\nFOR SHARE"));

        return rewritten;
    }

    public override string RenderFragment(ISqlFragment fragment, ISqlContext context)
    {
        if (fragment is SqlMultiTableUpdateFragment update)
        {
            // MySQL Block Swapping!
            var sql = $"{SqlKeyword.Update} {update.Target.ToSql(context)}";
            if (update.FromClause != null) sql += $", {update.FromClause.ToSql(context)}"; // Swap FROM for a comma
            
            sql += $"{Environment.NewLine}{SqlKeyword.Set} {update.SetClause.ToSql(context)}"; // SET moves to the middle
            
            if (update.WhereClause != null) sql += $"{Environment.NewLine}{SqlKeyword.Where} {update.WhereClause.ToSql(context)}";
            
            return sql;
        }

        // Just in case a lock fragment bypasses the rewriter, return empty
        if (fragment is SqlLockFragment) return string.Empty;
        
        return base.RenderFragment(fragment, context);
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