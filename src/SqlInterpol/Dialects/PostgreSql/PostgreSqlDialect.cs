using SqlInterpol.Configuration;
using SqlInterpol.Dialects.PostgreSql;
using SqlInterpol.Segments;

namespace SqlInterpol.Dialects;

/// <summary>
/// The PostgreSQL dialect: <c>$N</c>-style parameters, double-quote identifiers, and dialect-specific
/// rendering for row locking, multi-table DELETE (USING), and SELECT INTO.
/// </summary>
[SqlDialect(OpenQuote = _openQuote, CloseQuote = _closeQuote)]
public class PostgreSqlDialect : SqlDialectBase
{
    private const string _openQuote = "\"";
    private const string _closeQuote = "\"";

    /// <inheritdoc />
    public override SqlDialectKind Kind => SqlDialectKind.PostgreSql;
    
    /// <inheritdoc />
    public override string OpenQuote => _openQuote;
    
    /// <inheritdoc />
    public override string CloseQuote => _closeQuote;
    
    /// <inheritdoc />
    public override string ParameterPrefix => "$";
    
    private static readonly string[] PostgresSymbols = ["->>", "->", "@>", "<@"];
    
    /// <inheritdoc />
    public override IReadOnlySet<SqlFeature> SupportedFeatures { get; } =
        new HashSet<SqlFeature>
        {
            SqlFeature.ForUpdate,
            SqlFeature.ForShare,
            SqlFeature.Returning,
            SqlFeature.OnConflict,
            SqlFeature.SelectInto,
            SqlFeature.MultiTableDelete,
            SqlFeature.MultiTableUpdate,
            SqlFeature.DeleteAs,
            SqlFeature.UpdateAs
        };
    
    /// <inheritdoc />
    public override int QueryParametersMaxCount => 65535;

    /// <summary>
    /// Injects the PostgreSQL-specific syntax rewriter into the segment processing pipeline.
    /// </summary>
    public override SqlInterpolOptions GetDefaultOptions()
    {
        var options = base.GetDefaultOptions() with { ParameterIndexStart = 1 };
        options.Rewriters.Add(new PostgreSqlSyntaxRewriter());
        return options;
    }

    /// <inheritdoc />
    public override bool IsExpressionContext(string textBeforeParen)
    {
        foreach (var symbol in PostgresSymbols)
        {
            if (textBeforeParen.EndsWith(symbol)) return true;
        }

        return base.IsExpressionContext(textBeforeParen);
    }

    /// <inheritdoc />
    public override string RenderFragment(ISqlFragment fragment, ISqlContext context)
    {
        if (fragment is SqlLockFragment) return string.Empty;

        if (fragment is SqlMultiTableDeleteFragment delete)
        {
            return $"DELETE FROM {delete.Target.ToSql(context).Trim()}{Environment.NewLine}USING {delete.FromClause.ToSql(context).Trim()}" +
                (delete.WhereClause != null ? $"{Environment.NewLine}WHERE {delete.WhereClause.ToSql(context).Trim()}" : "");
        }

        return base.RenderFragment(fragment, context);
    }
}