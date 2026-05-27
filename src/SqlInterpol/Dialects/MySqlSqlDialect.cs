using SqlInterpol.Parsing;

namespace SqlInterpol.Dialects;

/// <summary>
/// The MySQL/MariaDB dialect: backtick identifiers, <c>@pN</c> parameters, ON DUPLICATE KEY UPDATE
/// emulation of upserts, and MySQL-style multi-table UPDATE/DELETE syntax.
/// </summary>
public class MySqlSqlDialect : SqlDialectBase
{
    public override SqlDialectKind Kind => SqlDialectKind.MySql;
    public override string OpenQuote => "`";
    public override string CloseQuote => "`";
    public override string ParameterPrefix => "@p";
    public override IReadOnlySet<SqlFeature> SupportedFeatures { get; } = new HashSet<SqlFeature>
    {
        SqlFeature.ForUpdate,
        SqlFeature.ForShare,
        SqlFeature.OnConflict, // Emulated via ON DUPLICATE KEY UPDATE
        SqlFeature.SelectInto
    };
    public override int QueryParametersMaxCount => 65535;

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

        if (deferredLock == SqlLockMode.Update)
            rewritten.Add(new SqlSegment(SqlSegmentType.Literal, "\nFOR UPDATE"));
        else if (deferredLock == SqlLockMode.Share)
            rewritten.Add(new SqlSegment(SqlSegmentType.Literal, "\nFOR SHARE"));

        return rewritten;
    }

    /// <inheritdoc />
    public override string RenderFragment(ISqlFragment fragment, ISqlContext context)
    {
        if (fragment is SqlMultiTableUpdateFragment update)
        {
            var sql = $"{SqlKeyword.Update} {update.Target.ToSql(context)}";
            if (update.FromClause != null) sql += $", {update.FromClause.ToSql(context)}";
            
            sql += $"{Environment.NewLine}{SqlKeyword.Set} {update.SetClause.ToSql(context)}";
            
            if (update.WhereClause != null) sql += $"{Environment.NewLine}{SqlKeyword.Where} {update.WhereClause.ToSql(context)}";
            
            return sql;
        }

        if (fragment is SqlMultiTableDeleteFragment delete)
        {
            var targetDecl = delete.Target.ToSql(context).Trim();
            var fromClause = delete.FromClause.ToSql(context).Trim();
            var whereClause = delete.WhereClause?.ToSql(context).Trim() ?? "1=1";

            var targetAliasMatch = System.Text.RegularExpressions.Regex.Match(targetDecl, @"AS\s+(`?[a-zA-Z0-9_]+`?)", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            var deleteTarget = targetAliasMatch.Success ? targetAliasMatch.Groups[1].Value : targetDecl;

            return $"DELETE {deleteTarget}{Environment.NewLine}FROM {targetDecl}, {fromClause}{Environment.NewLine}WHERE {whereClause}";
        }

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
            var seg = fragment.SourceSegments[i];
            vsb.Append(SqlSegmentRenderer.Instance.Render(context, seg, i, fragment.SourceSegments));
        }

        return vsb.ToString();
    }
}

/// <summary>
/// A helper fragment that strips the leading <c>SET</c> keyword from a <see cref="SqlSetFragment"/>
/// for use in MySQL's ON DUPLICATE KEY UPDATE clause.
/// </summary>
/// <param name="original">The SET fragment to wrap.</param>
public class MySqlUpdateFragment(SqlSetFragment original) : ISqlFragment
{
    /// <inheritdoc />
    public string ToSql(ISqlContext context, SqlRenderMode mode = SqlRenderMode.Default)
    {
        var sql = original.ToSql(context, mode);
        if (sql.StartsWith("SET", StringComparison.OrdinalIgnoreCase)) return " " + sql[3..].TrimStart();
        return " " + sql;
    }
}