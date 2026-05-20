using SqlInterpol.Config;
using SqlInterpol.Rendering;

namespace SqlInterpol.Dialects;

public class SqLiteSqlDialect : SqlDialectBase
{
    public override SqlDialectKind Kind => SqlDialectKind.SqLite;
    public override string OpenQuote => "\"";
    public override string CloseQuote => "\"";
    public override string ParameterPrefix => "?";
    public override IReadOnlySet<SqlFeature> SupportedFeatures { get; } = new HashSet<SqlFeature>
    {
        SqlFeature.Returning,
        SqlFeature.OnConflict,
        SqlFeature.SelectInto
    };

    public override IEnumerable<SqlSegment> RewriteSegments(IReadOnlyList<SqlSegment> segments)
    {
        var rewritten = base.RewriteSegments(segments).ToList();

        for (int i = 0; i < rewritten.Count; i++)
        {
            if (rewritten[i].Type == SqlSegmentType.Raw && rewritten[i].Value is SqlLockFragment)
            {
                // Just erase it from the AST to prevent SQLite syntax errors!
                rewritten[i] = new SqlSegment(SqlSegmentType.Literal, ""); 
            }
        }

        // Notice we do NOT append anything to the end here!
        return rewritten;
    }

    public override string RenderFragment(ISqlFragment fragment, ISqlContext context)
    {
        if (fragment is SqlLockFragment)
        {
            return string.Empty;
        }

        return base.RenderFragment(fragment, context);
    }

    protected override string RenderSelectInto(SqlSelectIntoFragment fragment, ISqlContext context)
    {
        string target = fragment.TargetTable switch
        {
            string s => QuoteIdentifier(s),
            SqlSegment paramSeg => SqlSegmentRenderer.Instance.Render(context, paramSeg, 0, [paramSeg]) ?? "",
            ISqlFragment frag => frag.ToSql(context),
            _ => fragment.TargetTable.ToString()!
        };

        var vsb = new System.Text.StringBuilder();
        vsb.AppendLine($"CREATE TABLE {target} AS");

        for (int i = 0; i < fragment.SourceSegments.Count; i++)
        {
            var seg = fragment.SourceSegments[i];
            vsb.Append(SqlSegmentRenderer.Instance.Render(context, seg, i, fragment.SourceSegments));
        }

        return vsb.ToString();
    }
}