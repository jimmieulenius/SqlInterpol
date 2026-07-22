using System.Collections.Generic;
using SqlInterpol.Configuration;
using SqlInterpol.Dialects.SqLite;
using SqlInterpol.Pipeline;
using SqlInterpol.Segments;

namespace SqlInterpol.Dialects;

/// <summary>
/// The SQLite dialect: double-quote identifiers, 1-based <c>@pN</c> parameters, 
/// and support for standard ON CONFLICT upsert syntax. Ignores unsupported locking hints.
/// </summary>
[SqlDialect(OpenQuote = _openQuote, CloseQuote = _closeQuote)]
public class SqLiteDialect : SqlDialectBase
{
    private const string _openQuote = "\"";
    private const string _closeQuote = "\"";

    /// <inheritdoc />
    public override SqlDialectKind Kind => SqlDialectKind.SqLite;
    
    /// <inheritdoc />
    public override string OpenQuote => _openQuote;
    
    /// <inheritdoc />
    public override string CloseQuote => _closeQuote;
    
    /// <inheritdoc />
    public override string ParameterPrefix => "@p";
    
    /// <inheritdoc />
    public override IReadOnlySet<SqlFeature> SupportedFeatures { get; } =
        new HashSet<SqlFeature>
        {
            SqlFeature.Returning,
            SqlFeature.OnConflict,
            SqlFeature.CreateTableAsSelect,
            SqlFeature.MultiTableUpdate,
            SqlFeature.MultiTableDelete
        };

    /// <summary>
    /// Injects the SQLite-specific syntax rewriter into the segment processing pipeline.
    /// </summary>
    public override SqlInterpolOptions GetDefaultOptions() 
    { 
        var options = base.GetDefaultOptions() with { ParameterIndexStart = 1 };
        options.Rewriters.Add(new SqLiteSyntaxRewriter());
        return options;
    }

    /// <inheritdoc />
    public override string RenderFragment(ISqlFragment fragment, ISqlContext context)
    {
        if (fragment is SqlLockFragment) return string.Empty;
        
        return base.RenderFragment(fragment, context);
    }

    /// <inheritdoc />
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