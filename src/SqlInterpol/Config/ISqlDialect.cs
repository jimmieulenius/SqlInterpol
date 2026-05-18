namespace SqlInterpol.Config;

public interface ISqlDialect
{    
    SqlDialectKind Kind { get; }
    string OpenQuote { get; }
    string CloseQuote { get; }
    string ParameterPrefix { get; }
    IReadOnlySet<SqlFeature> SupportedFeatures { get; }
    
    bool IsExpressionContext(string textBeforeParen);

    string QuoteIdentifier(string name);

    string UnquoteIdentifier(string identifier);

    string QuoteEntityName(string table, string? schema = null);
    
    string GetParameterName(int index);

    string ApplyAlias(string source, string? alias = null);

    string RenderFragment(ISqlFragment fragment, ISqlContext context);

    IEnumerable<SqlSegment> RewriteSegments(IReadOnlyList<SqlSegment> segments);

    SqlInterpolOptions GetDefaultOptions() => new();
}