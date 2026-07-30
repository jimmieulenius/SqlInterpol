using SqlInterpol.Configuration;
using SqlInterpol.Segments; 

namespace SqlInterpol.Testing.Xunit.Dialects;

/// <summary>
/// A completely vanilla, predictable SQL dialect intended solely for testing structural rewriters and AST generation.
/// Uses standard double quotes for identifiers and '@' for parameters.
/// </summary>
public sealed class MockDialect : ISqlDialect
{
    private static readonly IReadOnlySet<SqlFeature> _allFeatures = new HashSet<SqlFeature>(Enum.GetValues<SqlFeature>());

    /// <inheritdoc/>
    public SqlDialectKind Kind { get; } = new SqlDialectKind("Mock");

    /// <inheritdoc/>
    public string OpenQuote => "\"";

    /// <inheritdoc/>
    public string CloseQuote => "\"";

    /// <inheritdoc/>
    public string ParameterPrefix => "@";

    /// <inheritdoc/>
    public int QueryParametersMaxCount => 2100;

    /// <summary>
    /// The mock dialect supports all features by default so that tests do not unexpectedly throw NotSupportedExceptions.
    /// </summary>
    public IReadOnlySet<SqlFeature> SupportedFeatures => _allFeatures;

    /// <inheritdoc/>
    public bool IsExpressionContext(string keyword) => false;

    /// <inheritdoc/>
    public string QuoteIdentifier(string identifier) => $"{OpenQuote}{identifier}{CloseQuote}";

    /// <inheritdoc/>
    public string UnquoteIdentifier(string identifier) => identifier.Trim('\"');

    /// <inheritdoc/>
    public string QuoteEntityName(string name, string? schema) =>
        string.IsNullOrEmpty(schema) ? QuoteIdentifier(name) : $"{QuoteIdentifier(schema)}.{QuoteIdentifier(name)}";

    /// <inheritdoc/>
    public string GetParameterName(int index) => $"{ParameterPrefix}p{index}";

    /// <inheritdoc/>
    public string ApplyAlias(string expression, string? alias) =>
        string.IsNullOrEmpty(alias) ? expression : $"{expression} AS {QuoteIdentifier(alias)}";

    /// <inheritdoc/>
    public string RenderFragment(ISqlFragment fragment, ISqlContext context)
    {
        // For testing purposes, we can fall back to the fragment's default ToString implementation
        return fragment.ToString() ?? string.Empty;
    }
}