using System.Text.RegularExpressions;
using SqlInterpol.Configuration;
using SqlInterpol.Dialects.MySql;
using SqlInterpol.Pipeline;
using SqlInterpol.Segments;

namespace SqlInterpol.Dialects;

/// <summary>
/// The MySQL/MariaDB dialect: backtick identifiers, <c>@pN</c> parameters, ON DUPLICATE KEY UPDATE
/// emulation of upserts, and MySQL-style multi-table UPDATE/DELETE syntax.
/// </summary>
[SqlDialect(OpenQuote = _openQuote, CloseQuote = _closeQuote)]
public class MySqlDialect : SqlDialectBase
{
    private const string _openQuote = "`";
    private const string _closeQuote = "`";

    /// <inheritdoc />
    public override SqlDialectKind Kind => SqlDialectKind.MySql;
    
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
            SqlFeature.ForUpdate,
            SqlFeature.MultiTableUpdate,
            SqlFeature.MultiTableDelete,
            SqlFeature.UpdateAs,
            SqlFeature.UpdatableInlineViews,
            SqlFeature.CreateTableAsSelect,
            SqlFeature.DeleteAs
        };
    
    /// <inheritdoc />
    public override int QueryParametersMaxCount => 65535;

    /// <summary>
    /// Injects the MySQL-specific syntax rewriter into the segment processing pipeline.
    /// </summary>
    public override SqlInterpolOptions GetDefaultOptions()
    {
        var options = base.GetDefaultOptions();
        options.Rewriters.Add(new MySqlSyntaxRewriter());
        return options;
    }

    /// <inheritdoc />
    public override string RenderFragment(ISqlFragment fragment, ISqlContext context)
    {
        if (fragment is SqlMultiTableUpdateFragment update)
        {
            var setSql = update.SetClause.ToSql(context).Trim();
            string setPrefix = setSql.StartsWith("SET", StringComparison.OrdinalIgnoreCase) ? "" : $"{SqlKeyword.Set} ";
            
            var sql = $"{SqlKeyword.Update} {update.Target.ToSql(context).Trim()}";
            if (update.FromClause != null) sql += $", {update.FromClause.ToSql(context).Trim()}";
            
            sql += $"{Environment.NewLine}{setPrefix}{setSql}";
            if (update.WhereClause != null) sql += $"{Environment.NewLine}{SqlKeyword.Where} {update.WhereClause.ToSql(context).Trim()}";
            
            return sql;
        }

        if (fragment is SqlMultiTableDeleteFragment delete)
        {
            var targetDecl = delete.Target.ToSql(context).Trim();
            var fromClause = delete.FromClause.ToSql(context).Trim();
            var whereClause = delete.WhereClause?.ToSql(context).Trim() ?? "1=1";

            var targetAliasMatch = Regex.Match(targetDecl, @"AS\s+(`?[a-zA-Z0-9_]+`?)", RegexOptions.IgnoreCase);
            var deleteTarget = targetAliasMatch.Success ? targetAliasMatch.Groups[1].Value : targetDecl;

            return $"DELETE {deleteTarget}{Environment.NewLine}FROM {targetDecl}, {fromClause}{Environment.NewLine}WHERE {whereClause}";
        }

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

    /// <inheritdoc />
    protected override string RenderDeleteAs(SqlDeleteAsFragment fragment, ISqlContext context)
    {
        string alias = fragment.Target.ToSql(context, SqlRenderMode.AliasOnly);
        string baseName = fragment.Target.ToSql(context, SqlRenderMode.BaseName);
        
        // FIX: Appended ' AS {alias}' to correctly alias the table in the FROM clause
        return $"DELETE {alias} FROM {baseName} AS {alias}";
    }
}