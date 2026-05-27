
namespace SqlInterpol.Dialects;

/// <summary>
/// The PostgreSQL dialect: <c>$N</c>-style parameters, double-quote identifiers, and dialect-specific
/// rendering for row locking, multi-table DELETE (USING), and SELECT INTO.
/// </summary>
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
    public override int QueryParametersMaxCount => 65535;

    /// <inheritdoc />
    public override bool IsExpressionContext(string textBeforeParen)
    {
        foreach (var symbol in PostgresSymbols)
        {
            if (textBeforeParen.EndsWith(symbol)) return true;
        }

        return base.IsExpressionContext(textBeforeParen);
    }

    /// <inheritdoc />
    public override SqlInterpolOptions GetDefaultOptions() => new() 
    { 
        ParameterIndexStart = 1 
    };

    /// <inheritdoc />
    public override IEnumerable<SqlSegment> RewriteSegments(IReadOnlyList<SqlSegment> segments)
    {
        var rewritten = base.RewriteSegments(segments).ToList();
        
        SqlLockMode? deferredLock = null;

        for (int i = 0; i < rewritten.Count; i++)
        {
            var segment = rewritten[i];
            
            if (segment.Type == SqlSegmentType.Raw && segment.Value is SqlLockFragment lockFrag)
            {
                deferredLock = lockFrag.Mode;
                rewritten[i] = new SqlSegment(SqlSegmentType.Literal, ""); 
            }
        }

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

    /// <inheritdoc />
    public override string RenderFragment(ISqlFragment fragment, ISqlContext context)
    {
        if (fragment is SqlLockFragment) return string.Empty;

        if (fragment is SqlMultiTableDeleteFragment delete)
        {
            return $"DELETE FROM {delete.Target.ToSql(context).Trim()}{Environment.NewLine}USING {delete.FromClause.ToSql(context).Trim()}" +
                (delete.WhereClause != null ? $"{Environment.NewLine}WHERE {delete.WhereClause.ToSql(context).Trim()}" : "");
        }

        return base.RenderFragment(fragment, context);
    }
}