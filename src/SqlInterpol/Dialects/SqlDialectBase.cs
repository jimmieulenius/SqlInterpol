using System.Text.RegularExpressions;

namespace SqlInterpol.Dialects;

/// <summary>
/// Abstract base class for all SQL dialect implementations, providing ANSI-compatible
/// identifier quoting, parameter naming, expression context detection, and pure text rendering.
/// </summary>
public abstract class SqlDialectBase : ISqlDialect
{
    /// <inheritdoc />
    public abstract SqlDialectKind Kind { get; }
    
    /// <inheritdoc />
    public abstract string OpenQuote { get; }
    
    /// <inheritdoc />
    public abstract string CloseQuote { get; }
    
    /// <inheritdoc />
    public abstract string ParameterPrefix { get; }

    /// <summary>
    /// Operators that indicate an open expression context.
    /// </summary>
    protected static readonly string[] DefaultExpressionSymbols = ["=", "<", ">", "<=", ">=", "<>", "!=", "+", "-", "*", "/", "%"];
    
    /// <summary>
    /// Keywords that indicate an open expression context.
    /// </summary>
    protected static readonly string[] DefaultExpressionKeywords = [SqlKeyword.In, SqlKeyword.Exists, SqlKeyword.Any, SqlKeyword.All, SqlKeyword.Some];

    /// <inheritdoc />
    public virtual IReadOnlySet<SqlFeature> SupportedFeatures { get; } = new HashSet<SqlFeature>();
    
    /// <inheritdoc />
    public virtual int QueryParametersMaxCount => 999;


    /// <inheritdoc />
    public virtual SqlInterpolOptions GetDefaultOptions() => new();

    /// <inheritdoc />
    public virtual string QuoteIdentifier(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return name;
        var trimmed = name.Trim();
        if (trimmed.Length < 2 || !trimmed.StartsWith(OpenQuote) || !trimmed.EndsWith(CloseQuote)) return $"{OpenQuote}{trimmed}{CloseQuote}";
        return trimmed;
    }

    /// <inheritdoc />
    public virtual string UnquoteIdentifier(string identifier)
    {
        if (string.IsNullOrEmpty(identifier)) return identifier;
        string open = OpenQuote; string close = CloseQuote;
        if (identifier.StartsWith(open) && identifier.EndsWith(close)) return identifier.Substring(open.Length, identifier.Length - open.Length - close.Length);
        return identifier;
    }

    /// <inheritdoc />
    public virtual string QuoteEntityName(string table, string? schema = null)
    {
        var quotedTable = QuoteIdentifier(table);
        if (string.IsNullOrWhiteSpace(schema)) return quotedTable;
        return $"{QuoteIdentifier(schema)}.{quotedTable}";
    }

    /// <inheritdoc />
    public virtual string GetParameterName(int index) => $"{ParameterPrefix}{index}";

    /// <inheritdoc />
    public virtual bool IsExpressionContext(string textBeforeParen)
    {
        if (string.IsNullOrWhiteSpace(textBeforeParen)) return false;
        foreach (var symbol in DefaultExpressionSymbols) if (textBeforeParen.EndsWith(symbol)) return true;
        int lastSeparator = textBeforeParen.LastIndexOfAny([' ', '\t', '\n', '\r', '(']);
        string lastWord = lastSeparator >= 0 ? textBeforeParen[(lastSeparator + 1)..] : textBeforeParen;
        foreach (var keyword in DefaultExpressionKeywords) if (lastWord.Equals(keyword, StringComparison.OrdinalIgnoreCase)) return true;
        return false;
    }

    /// <inheritdoc />
    public virtual string ApplyAlias(string source, string? alias = null)
    {
        if (string.IsNullOrWhiteSpace(alias)) return source;
        return $"{QuoteIdentifier(source)} {SqlKeyword.As.Value} {alias}";
    }

    /// <inheritdoc />
    public virtual string RenderFragment(ISqlFragment fragment, ISqlContext context)
    {
        return fragment switch
        {
            SqlDeleteAsFragment delAs => RenderDeleteAs(delAs, context),
            SqlUpdateAsFragment upAs => RenderUpdateAs(upAs, context),
            SqlPagingFragment p => $"{SqlKeyword.Limit} {p.Limit} {SqlKeyword.Offset} {p.Offset}",
            SqlSetOperationFragment setOp => RenderSetOperation(setOp, context),
            SqlMultiTableUpdateFragment update => RenderMultiTableUpdate(update, context),
            SqlMultiTableDeleteFragment delete => RenderMultiTableDelete(delete, context),
            SqlSelectIntoFragment selectInto => RenderSelectInto(selectInto, context),
            SqlUpdateSubqueryFragment upSub => RenderUpdateSubquery(upSub, context),
            SqlUpdateCteFragment upCte => RenderUpdateCte(upCte, context),
            _ => throw new NotSupportedException($"The fragment type '{fragment.GetType().Name}' is not supported by {this.GetType().Name}.")
        };
    }

    /// <summary>
    /// Renders a mathematical or relational set operation (UNION, EXCEPT, INTERSECT) between two queries.
    /// </summary>
    protected virtual string RenderSetOperation(SqlSetOperationFragment fragment, ISqlContext context)
    {
        string opKeyword = fragment.Operator switch
        {
            SqlSetOperator.Except => SqlKeyword.Except,
            SqlSetOperator.Intersect => SqlKeyword.Intersect,
            SqlSetOperator.Union => SqlKeyword.Union,
            SqlSetOperator.UnionAll => SqlKeyword.UnionAll,
            _ => throw new NotImplementedException($"Set operator {fragment.Operator} is not supported.")
        };
        return $"{fragment.Left.ToSql(context)}{Environment.NewLine}{opKeyword}{Environment.NewLine}{fragment.Right.ToSql(context)}";
    }

    /// <summary>
    /// Renders an ANSI-standard multi-table UPDATE statement utilizing a FROM clause.
    /// </summary>
    protected virtual string RenderMultiTableUpdate(SqlMultiTableUpdateFragment update, ISqlContext context)
    {
        var targetSql = update.Target.ToSql(context).Trim();
        var setSql = update.SetClause.ToSql(context).Trim();
        string setPrefix = setSql.StartsWith("SET", StringComparison.OrdinalIgnoreCase) ? "" : "SET ";
        
        var sql = $"UPDATE {targetSql}{Environment.NewLine}{setPrefix}{setSql}";
        if (update.FromClause != null) sql += $"{Environment.NewLine}FROM {update.FromClause.ToSql(context).Trim()}";
        if (update.WhereClause != null) sql += $"{Environment.NewLine}WHERE {update.WhereClause.ToSql(context).Trim()}";
            
        return sql;
    }

    /// <summary>
    /// Renders an ANSI-standard multi-table DELETE statement utilizing a FROM clause.
    /// </summary>
    protected virtual string RenderMultiTableDelete(SqlMultiTableDeleteFragment delete, ISqlContext context)
    {
        var targetSql = delete.Target.ToSql(context).Trim();
        var sql = $"DELETE FROM {targetSql}";
        
        if (delete.FromClause != null) sql += $"{Environment.NewLine}FROM {delete.FromClause.ToSql(context).Trim()}";
        if (delete.WhereClause != null) sql += $"{Environment.NewLine}WHERE {delete.WhereClause.ToSql(context).Trim()}";
            
        return sql;
    }

    /// <summary>
    /// Renders a DELETE statement targeting an aliased table.
    /// </summary>
    protected virtual string RenderDeleteAs(SqlDeleteAsFragment fragment, ISqlContext context)
    {
        string baseName = fragment.Target.ToSql(context, SqlRenderMode.BaseName);
        string alias = context.Dialect.QuoteIdentifier(fragment.Target.Reference.Alias ?? "tgt");
        return $"DELETE FROM {baseName} AS {alias}";
    }

    /// <summary>
    /// Renders an UPDATE statement targeting an aliased table.
    /// </summary>
    protected virtual string RenderUpdateAs(SqlUpdateAsFragment fragment, ISqlContext context)
    {
        string baseName = fragment.Target.ToSql(context, SqlRenderMode.BaseName);
        string alias = context.Dialect.QuoteIdentifier(fragment.Target.Reference.Alias ?? "tgt");
        return $"UPDATE {baseName} AS {alias}";
    }

    /// <summary>
    /// Renders an UPDATE statement targeting an inline view (subquery).
    /// </summary>
    protected virtual string RenderUpdateSubquery(SqlUpdateSubqueryFragment fragment, ISqlContext context)
    {
        var quotedAlias = QuoteIdentifier(fragment.Alias);
        var subquerySql = Regex.Replace(fragment.Subquery.ToSql(context), @"\s+", " ").Trim();
        
        var setSql = fragment.SetClause.ToSql(context).Trim();
        string setPrefix = setSql.StartsWith("SET", StringComparison.OrdinalIgnoreCase) ? "" : "SET ";
        
        var sql = $"UPDATE ({subquerySql}) AS {quotedAlias}{Environment.NewLine}{setPrefix}{setSql}";
        if (fragment.WhereClause != null) sql += $"{Environment.NewLine}WHERE {fragment.WhereClause.ToSql(context).Trim()}";
        return sql;
    }

    /// <summary>
    /// Renders an UPDATE statement by elevating an inline view to a Common Table Expression (CTE).
    /// Used as a fallback for dialects that do not natively support updatable inline views.
    /// </summary>
    protected virtual string RenderUpdateCte(SqlUpdateCteFragment fragment, ISqlContext context)
    {
        var quotedAlias = QuoteIdentifier(fragment.Alias);
        var indent = new string(' ', context.Options.IndentSize);
        var subquerySql = fragment.Subquery.ToSql(context).Trim();
        
        var indentedSubquery = indent + subquerySql.Replace("\n", $"\n{indent}");
        
        var setSql = fragment.SetClause.ToSql(context).Trim();
        string setPrefix = setSql.StartsWith("SET", StringComparison.OrdinalIgnoreCase) ? "" : "SET ";

        var sql = $"WITH {quotedAlias} AS ({Environment.NewLine}{indentedSubquery}{Environment.NewLine}){Environment.NewLine}UPDATE {quotedAlias}{Environment.NewLine}{setPrefix}{setSql}";
        if (fragment.WhereClause != null) sql += $"{Environment.NewLine}WHERE {fragment.WhereClause.ToSql(context).Trim()}";
        return sql;
    }

    /// <summary>
    /// Renders a SELECT INTO statement for rapid table generation and bulk insertion.
    /// </summary>
    protected virtual string RenderSelectInto(SqlSelectIntoFragment fragment, ISqlContext context)
    {
        string target = fragment.TargetTable switch
        {
            string s => QuoteIdentifier(s),
            SqlSegment paramSeg => SqlSegmentRenderer.Instance.Render(context, paramSeg, 0, [paramSeg]) ?? "",
            ISqlFragment frag => frag.ToSql(context),
            _ => fragment.TargetTable.ToString()!
        };

        var vsb = new System.Text.StringBuilder();
        for (int i = 0; i < fragment.SourceSegments.Count; i++)
        {
            if (i == fragment.IntoSegmentIndex) vsb.Append($"{Environment.NewLine}INTO {target}");
            var seg = fragment.SourceSegments[i];
            vsb.Append(SqlSegmentRenderer.Instance.Render(context, seg, i, fragment.SourceSegments));
        }
        if (fragment.IntoSegmentIndex >= fragment.SourceSegments.Count) vsb.Append($"{Environment.NewLine}INTO {target}");
        return vsb.ToString();
    }
}