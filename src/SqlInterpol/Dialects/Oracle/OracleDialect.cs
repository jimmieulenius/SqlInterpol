using System.Text.RegularExpressions;
using SqlInterpol.Dialects.Oracle;
using SqlInterpol.Dialects;

namespace SqlInterpol;

/// <summary>
/// The Oracle dialect: colon-prefixed parameters, double-quote identifiers, OFFSET/FETCH paging,
/// MERGE-based multi-table UPDATE, EXISTS-based multi-table DELETE, and MINUS for EXCEPT.
/// </summary>
public class OracleDialect : SqlDialectBase
{
    /// <inheritdoc />
    public override SqlDialectKind Kind => SqlDialectKind.Oracle;
    
    /// <inheritdoc />
    public override string OpenQuote => "\"";
    
    /// <inheritdoc />
    public override string CloseQuote => "\"";
    
    /// <inheritdoc />
    public override string ParameterPrefix => ":";
    
    /// <inheritdoc />
    public override IReadOnlySet<SqlFeature> SupportedFeatures { get; } =
        new HashSet<SqlFeature>
        {
            SqlFeature.ForUpdate,
            SqlFeature.Returning,
            SqlFeature.CreateTableAsSelect,
            SqlFeature.UpdatableInlineViews,
            SqlFeature.MultiTableUpdate,
            SqlFeature.MultiTableDelete
        };
    
    /// <inheritdoc />
    public override int QueryParametersMaxCount => 65535;

    /// <summary>
    /// Injects the Oracle-specific syntax rewriter into the compilation pipeline.
    /// </summary>
    public override SqlInterpolOptions GetDefaultOptions()
    {
        var options = base.GetDefaultOptions();
        options.Rewriters.Add(new OracleSyntaxRewriter());
        return options;
    }

    /// <inheritdoc />
    public override string ApplyAlias(string source, string? alias = null)
    {
        if (string.IsNullOrWhiteSpace(alias)) return source;
        
        // Safely bypass quoting if the source is already an inline view / subquery.
        string safeSource = string.IsNullOrWhiteSpace(source) ? "" : (source.TrimStart().StartsWith("(") ? source : QuoteIdentifier(source));
        
        // Oracle uniquely drops the 'AS' keyword for table aliases
        return string.IsNullOrWhiteSpace(safeSource) ? alias : $"{safeSource} {alias}";
    }

    /// <inheritdoc />
    public override string RenderFragment(ISqlFragment fragment, ISqlContext context)
    {
        if (fragment is SqlPagingFragment p)
        {
            return $"OFFSET {p.Offset} ROWS FETCH NEXT {p.Limit} ROWS ONLY";
        }
        
        if (fragment is SqlLockFragment) return string.Empty;

        if (fragment is SqlSetOperationFragment setOp && setOp.Operator == SqlSetOperator.Except)
        {
            return $"{setOp.Left.ToSql(context)}{Environment.NewLine}MINUS{Environment.NewLine}{setOp.Right.ToSql(context)}";
        }

        if (fragment is SqlMultiTableUpdateFragment update && update.FromClause != null)
        {
            var target = update.Target.ToSql(context).Trim();
            var setClause = update.SetClause.ToSql(context).Trim();
            var fromClause = Regex.Replace(update.FromClause.ToSql(context).Trim(), @"(?i)\bAS\b", "").Replace("  ", " ");
            var whereClause = update.WhereClause?.ToSql(context).Trim() ?? "1=1";

            return $"MERGE INTO {target}{Environment.NewLine}USING {fromClause}{Environment.NewLine}ON ({whereClause}){Environment.NewLine}WHEN MATCHED THEN UPDATE SET {setClause}";
        }

        if (fragment is SqlMultiTableDeleteFragment delete)
        {
            var targetDecl = delete.Target.ToSql(context).Trim();
            var fromClause = Regex.Replace(delete.FromClause.ToSql(context).Trim(), @"(?i)\bAS\b", "").Replace("  ", " ");
            var whereClause = delete.WhereClause?.ToSql(context).Trim() ?? "1=1";
            var indent = new string(' ', context.Options.IndentSize);

            return $"DELETE FROM {targetDecl}{Environment.NewLine}WHERE EXISTS ({Environment.NewLine}{indent}SELECT 1{Environment.NewLine}{indent}FROM {fromClause}{Environment.NewLine}{indent}WHERE {whereClause}{Environment.NewLine})";
        }

        if (fragment is SqlUpdateSubqueryFragment upSub)
        {
            var quotedAlias = QuoteIdentifier(upSub.Alias);
            var subquerySql = Regex.Replace(upSub.Subquery.ToSql(context), @"\s+", " ").Trim();
            var setSql = upSub.SetClause.ToSql(context).Trim();
            string setPrefix = setSql.StartsWith("SET", StringComparison.OrdinalIgnoreCase) ? "" : "SET ";

            var sql = $"UPDATE ({subquerySql}) {quotedAlias}{Environment.NewLine}{setPrefix}{setSql}";
            if (upSub.WhereClause != null) sql += $"{Environment.NewLine}WHERE {upSub.WhereClause.ToSql(context).Trim()}";
            return sql;
        }

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
            var seg = fragment.SourceSegments[i];
            vsb.Append(SqlSegmentRenderer.Instance.Render(context, seg, i, fragment.SourceSegments));
        }

        return vsb.ToString();
    }
}