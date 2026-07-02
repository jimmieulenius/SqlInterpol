using SqlInterpol.Dialects;
using SqlInterpol.Dialects.SqLite;

namespace SqlInterpol;

public class SqLiteDialect : SqlDialectBase
{
    public override SqlDialectKind Kind => SqlDialectKind.SqLite;
    public override string OpenQuote => "\"";
    public override string CloseQuote => "\"";
    public override string ParameterPrefix => "@p";
    public override IReadOnlySet<SqlFeature> SupportedFeatures { get; } =
        new HashSet<SqlFeature>
        {
            SqlFeature.Returning,
            SqlFeature.OnConflict,
            SqlFeature.CreateTableAsSelect,
            SqlFeature.MultiTableUpdate,
            SqlFeature.MultiTableDelete
        };

    public override SqlInterpolOptions GetDefaultOptions() 
    { 
        var options = base.GetDefaultOptions() with { ParameterIndexStart = 1 };
        options.Rewriters.Add(new SqLiteSyntaxRewriter());
        return options;
    }

    public override string RenderFragment(ISqlFragment fragment, ISqlContext context)
    {
        if (fragment is SqlLockFragment) return string.Empty;
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
            vsb.Append(SqlSegmentRenderer.Instance.Render(context, fragment.SourceSegments[i], i, fragment.SourceSegments));
        }

        return vsb.ToString();
    }
}