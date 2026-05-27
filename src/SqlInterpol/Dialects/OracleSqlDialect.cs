using SqlInterpol.Parsing;

namespace SqlInterpol.Dialects;

/// <summary>
/// The Oracle dialect: colon-prefixed parameters, double-quote identifiers, OFFSET/FETCH paging,
/// MERGE-based multi-table UPDATE, EXISTS-based multi-table DELETE, and MINUS for EXCEPT.
/// </summary>
public class OracleSqlDialect : SqlDialectBase
{
    public override SqlDialectKind Kind => SqlDialectKind.Oracle;
    public override string OpenQuote => "\"";
    public override string CloseQuote => "\"";
    public override string ParameterPrefix => ":";
    public override IReadOnlySet<SqlFeature> SupportedFeatures { get; } = new HashSet<SqlFeature>
    {
        SqlFeature.ForUpdate,
        SqlFeature.Returning,
        SqlFeature.SelectInto
    };
    public override int QueryParametersMaxCount => 65535;

    /// <inheritdoc />
    public override string RenderFragment(ISqlFragment fragment, ISqlContext context)
    {
        if (fragment is SqlPagingFragment p)
        {
            return $"OFFSET {p.Offset} ROWS FETCH NEXT {p.Limit} ROWS ONLY";
        }
        
        if (fragment is SqlLockFragment)
        {
            return string.Empty;
        }

        if (fragment is SqlSetOperationFragment setOp && setOp.Operator == SqlSetOperator.Except)
        {
            return $"{setOp.Left.ToSql(context)}{Environment.NewLine}MINUS{Environment.NewLine}{setOp.Right.ToSql(context)}";
        }

        if (fragment is SqlMultiTableUpdateFragment update && update.FromClause != null)
        {
            var target = update.Target.ToSql(context).Trim();
            var setClause = update.SetClause.ToSql(context).Trim();
            var fromClause = update.FromClause.ToSql(context).Trim();
            var whereClause = update.WhereClause?.ToSql(context).Trim() ?? "1=1";

            fromClause = SqlInterpolationParser.Instance.ReplaceKeyword(fromClause, "AS", "").Replace("  ", " ");

            return $"MERGE INTO {target}{Environment.NewLine}USING {fromClause}{Environment.NewLine}ON ({whereClause}){Environment.NewLine}WHEN MATCHED THEN UPDATE SET {setClause}";
        }

        if (fragment is SqlMultiTableDeleteFragment delete)
        {
            var targetDecl = delete.Target.ToSql(context).Trim();
            var fromClause = delete.FromClause.ToSql(context).Trim();
            var whereClause = delete.WhereClause?.ToSql(context).Trim() ?? "1=1";

            fromClause = SqlInterpolationParser.Instance.ReplaceKeyword(fromClause, "AS", "").Replace("  ", " ");
            var indent = new string(' ', context.Options.IndentSize);

            return $"DELETE FROM {targetDecl}{Environment.NewLine}WHERE EXISTS ({Environment.NewLine}{indent}SELECT 1{Environment.NewLine}{indent}FROM {fromClause}{Environment.NewLine}{indent}WHERE {whereClause}{Environment.NewLine})";
        }

        return base.RenderFragment(fragment, context);
    }

    /// <inheritdoc />
    public override IEnumerable<SqlSegment> RewriteSegments(IReadOnlyList<SqlSegment> segments)
    {
        var baseRewritten = base.RewriteSegments(segments).ToList();
        var rewritten = new List<SqlSegment>(baseRewritten.Count);
        
        SqlLockMode? deferredLock = null;

        for (int i = 0; i < baseRewritten.Count; i++)
        {
            var segment = baseRewritten[i];

            if (segment.Type == SqlSegmentType.Raw && segment.Value is SqlLockFragment lockFrag)
            {
                deferredLock = lockFrag.Mode;
                continue;
            }

            if (segment.Tag == SqlSegmentTag.Paging && segment.Value is string pagingValue)
            {
                if (i + 3 < baseRewritten.Count &&
                    baseRewritten[i + 1].Type == SqlSegmentType.Parameter &&
                    baseRewritten[i + 3].Type == SqlSegmentType.Parameter)
                {
                    int index = pagingValue.LastIndexOf(SqlKeyword.Limit, StringComparison.OrdinalIgnoreCase);
                    
                    if (index > -1)
                    {
                        rewritten.Add(new SqlSegment(SqlSegmentType.Literal, pagingValue[..index]));
                    }

                    rewritten.Add(new SqlSegment(SqlSegmentType.Literal, $"{SqlKeyword.Offset} "));
                    rewritten.Add(baseRewritten[i + 3]);
                    
                    rewritten.Add(new SqlSegment(SqlSegmentType.Literal, " ROWS FETCH NEXT "));
                    rewritten.Add(baseRewritten[i + 1]);
                    
                    rewritten.Add(new SqlSegment(SqlSegmentType.Literal, " ROWS ONLY"));

                    i += 3;

                    continue;
                }
            }

            if (segment.Type == SqlSegmentType.Literal && segment.Value is string literalValue)
            {
                var newValue = literalValue;

                if (newValue.Contains("WITH RECURSIVE", System.StringComparison.OrdinalIgnoreCase))
                    newValue = SqlInterpolationParser.Instance.ReplaceKeyword(newValue, "WITH RECURSIVE", "WITH");

                if (newValue.Contains("EXCEPT", System.StringComparison.OrdinalIgnoreCase))
                    newValue = SqlInterpolationParser.Instance.ReplaceKeyword(newValue, SqlKeyword.Except, "MINUS");

                if (!ReferenceEquals(newValue, literalValue))
                    segment = new SqlSegment(SqlSegmentType.Literal, newValue);
            }

            rewritten.Add(segment);
        }

        if (deferredLock == SqlLockMode.Update || deferredLock == SqlLockMode.Share)
        {
            rewritten.Add(new SqlSegment(SqlSegmentType.Literal, "\nFOR UPDATE"));
        }

        return rewritten;
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