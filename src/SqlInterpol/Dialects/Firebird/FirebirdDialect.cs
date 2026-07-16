using System.Text.RegularExpressions;
using SqlInterpol.Dialects;
using SqlInterpol.Dialects.Firebird;

namespace SqlInterpol;

/// <summary>
/// The Firebird dialect: positional parameters, strict maximum parameter limits,
/// FIRST/SKIP pagination layout, and explicit feature gatekeeping.
/// </summary>
[SqlDialect(OpenQuote = _openQuote, CloseQuote = _closeQuote)]
public partial class FirebirdDialect : SqlDialectBase
{
    private const string _openQuote = "\"";
    private const string _closeQuote = "\"";

    [GeneratedRegex(@"\bAS\b", RegexOptions.IgnoreCase)]
    private static partial Regex AsKeywordRegex();

    /// <inheritdoc />
    public override SqlDialectKind Kind => SqlDialectKind.Firebird;
    
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
            SqlFeature.Returning
        };
        
    /// <inheritdoc />
    public override int QueryParametersMaxCount => 1499;

    /// <inheritdoc />
    public override SqlInterpolOptions GetDefaultOptions()
    {
        var options = base.GetDefaultOptions();
        options.Rewriters.Add(new FirebirdSyntaxRewriter());
        return options;
    }

    /// <inheritdoc />
    public override string RenderFragment(ISqlFragment fragment, ISqlContext context)
    {
        if (fragment is SqlPagingFragment p) return $"FIRST {p.Limit} SKIP {p.Offset}";
        if (fragment is SqlLockFragment) return string.Empty;

        if (fragment is SqlMultiTableUpdateFragment update && update.FromClause != null)
        {
            var target = update.Target.ToSql(context).Trim();
            var setClause = update.SetClause.ToSql(context).Trim();
            var fromClause = AsKeywordRegex().Replace(update.FromClause.ToSql(context).Trim(), "").Replace("  ", " ");
            var whereClause = update.WhereClause?.ToSql(context).Trim() ?? "1=1";

            return $"MERGE INTO {target}{Environment.NewLine}USING {fromClause}{Environment.NewLine}ON ({whereClause}){Environment.NewLine}WHEN MATCHED THEN UPDATE SET {setClause}";
        }

        if (fragment is SqlMultiTableDeleteFragment delete)
        {
            var targetDecl = delete.Target.ToSql(context).Trim();
            var fromClause = AsKeywordRegex().Replace(delete.FromClause.ToSql(context).Trim(), "").Replace("  ", " ");
            var whereClause = delete.WhereClause?.ToSql(context).Trim() ?? "1=1";
            var indent = new string(' ', context.Options.IndentSize);

            return $"DELETE FROM {targetDecl}{Environment.NewLine}WHERE EXISTS ({Environment.NewLine}{indent}SELECT 1{Environment.NewLine}{indent}FROM {fromClause}{Environment.NewLine}{indent}WHERE {whereClause}{Environment.NewLine})";
        }

        return base.RenderFragment(fragment, context);
    }
}