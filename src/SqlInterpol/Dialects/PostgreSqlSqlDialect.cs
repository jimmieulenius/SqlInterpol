using SqlInterpol.Config;

namespace SqlInterpol.Dialects;

public class PostgreSqlSqlDialect : SqlDialectBase
{
    public override SqlDialectKind Kind => SqlDialectKind.PostgreSql;
    public override string OpenQuote => "\"";
    public override string CloseQuote => "\"";
    public override string ParameterPrefix => "$";
    private static readonly string[] PostgresSymbols = ["->>", "->", "@>", "<@"];
    public override IReadOnlySet<SqlFeature> SupportedFeatures { get; } = new HashSet<SqlFeature>
    {
        SqlFeature.ForUpdate,
        SqlFeature.ForShare,
        SqlFeature.Returning,
        SqlFeature.OnConflict,
        SqlFeature.SelectInto
    };

    public override bool IsExpressionContext(string textBeforeParen)
    {
        // 1. Check Postgres-specific symbols first
        foreach (var symbol in PostgresSymbols)
        {
            if (textBeforeParen.EndsWith(symbol)) return true;
        }

        // 2. Fall back to the core engine's ANSI checks
        return base.IsExpressionContext(textBeforeParen);
    }

    public override SqlInterpolOptions GetDefaultOptions() => new() 
    { 
        ParameterIndexStart = 1 
    };

    public override IEnumerable<SqlSegment> RewriteSegments(IReadOnlyList<SqlSegment> segments)
    {
        // 1. Let the base dialect run first (this converts "FOR UPDATE" text into SqlLockFragments)
        var rewritten = base.RewriteSegments(segments).ToList();
        
        SqlLockMode? deferredLock = null;

        // 2. Scan the AST for locks and extract them
        for (int i = 0; i < rewritten.Count; i++)
        {
            var segment = rewritten[i];
            
            if (segment.Type == SqlSegmentType.Raw && segment.Value is SqlLockFragment lockFrag)
            {
                deferredLock = lockFrag.Mode;
                
                // Erase the lock from its current position in the middle of the query
                rewritten[i] = new SqlSegment(SqlSegmentType.Literal, ""); 
            }
        }

        // 3. Append the lock to the absolute end of the query!
        if (deferredLock == SqlLockMode.Update)
        {
            rewritten.Add(new SqlSegment(SqlSegmentType.Literal, "\nFOR UPDATE"));
        }
        else if (deferredLock == SqlLockMode.Share)
        {
            rewritten.Add(new SqlSegment(SqlSegmentType.Literal, "\nFOR SHARE"));
        }

        return rewritten;
    }

    public override string RenderFragment(ISqlFragment fragment, ISqlContext context)
    {
        // Postgres doesn't render locks inline, but if it accidentally hits one, return empty
        if (fragment is SqlLockFragment) return string.Empty;

        if (fragment is SqlMultiTableDeleteFragment delete)
        {
            return $"DELETE FROM {delete.Target.ToSql(context).Trim()}{Environment.NewLine}USING {delete.FromClause.ToSql(context).Trim()}" +
                (delete.WhereClause != null ? $"{Environment.NewLine}WHERE {delete.WhereClause.ToSql(context).Trim()}" : "");
        }

        return base.RenderFragment(fragment, context);
    }
}