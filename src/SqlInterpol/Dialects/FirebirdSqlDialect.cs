using SqlInterpol.Config;

namespace SqlInterpol.Dialects;

public class FirebirdSqlDialect : SqlDialectBase
{
    public override SqlDialectKind Kind => SqlDialectKind.Firebird;
    public override string OpenQuote => "\"";
    public override string CloseQuote => "\"";
    public override string ParameterPrefix => "@p";
    public override IReadOnlySet<SqlFeature> SupportedFeatures { get; } = new HashSet<SqlFeature>
    {
        SqlFeature.ForUpdate,
        SqlFeature.Returning,
    };

    public override IEnumerable<SqlSegment> RewriteSegments(IReadOnlyList<SqlSegment> segments)
    {
        var baseRewritten = base.RewriteSegments(segments).ToList();
        var rewritten = new List<SqlSegment>(baseRewritten.Count);

        SqlLockMode? deferredLock = null;

        for (int i = 0; i < baseRewritten.Count; i++)
        {
            var segment = baseRewritten[i];

            // Extract and swallow Lock Fragments — Firebird uses WITH LOCK appended at the end
            if (segment.Type == SqlSegmentType.Raw && segment.Value is SqlLockFragment lockFrag)
            {
                deferredLock = lockFrag.Mode;
                continue;
            }

            rewritten.Add(segment);
        }

        // Append WITH LOCK after the full query (Firebird only supports row-level locking via WITH LOCK)
        if (deferredLock == SqlLockMode.Update)
        {
            rewritten.Add(new SqlSegment(SqlSegmentType.Literal, "\nWITH LOCK"));
        }

        return rewritten;
    }

    public override string RenderFragment(ISqlFragment fragment, ISqlContext context)
    {
        if (fragment is SqlPagingFragment p)
        {
            // Firebird uses 1-based ROWS m TO n syntax
            int from = p.Offset + 1;
            int to   = p.Offset + p.Limit;
            return $"ROWS {from} TO {to}";
        }

        if (fragment is SqlLockFragment)
        {
            return string.Empty;
        }

        return base.RenderFragment(fragment, context);
    }
}
