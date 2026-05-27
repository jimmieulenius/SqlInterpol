using SqlInterpol.Parsing;

namespace SqlInterpol;

/// <summary>
/// Default implementation of <see cref="ISqlContext"/>, holding the runtime state for a
/// <see cref="SqlBuilder"/> query build: dialect, parser, renderer, options, and parameters.
/// </summary>
public class SqlContext(SqlBuilder builder, ISqlDialect dialect, ISqlInterpolationParser parser, ISqlSegmentRenderer renderer, SqlInterpolOptions? options = null) : ISqlParserContext
{
    /// <summary>Gets the <see cref="SqlBuilder"/> that owns this context.</summary>
    public SqlBuilder Builder { get; } = builder;

    /// <summary>Gets the active SQL dialect.</summary>
    public ISqlDialect Dialect { get; } = dialect;

    /// <summary>Gets the parser used to process SQL literals and interpolated values.</summary>
    public ISqlInterpolationParser Parser { get; } = parser;

    /// <summary>Gets the renderer used to convert segments to SQL strings.</summary>
    public ISqlSegmentRenderer Renderer { get; } = renderer;

    /// <summary>Gets the configuration options for this context.</summary>
    public SqlInterpolOptions Options { get; } = options ?? new() { Dialect = dialect.Kind };

    /// <summary>Gets the accumulated dictionary of named parameters extracted from interpolated values.</summary>
    public IDictionary<string, object?> Parameters { get; private set; } = new Dictionary<string, object?>();

    internal SqlParserState ParserState { get; } = new();

    ISqlParserState ISqlParserContext.ParserState => ParserState;

    /// <inheritdoc />
    public string AddParameter(object? value)
    {
        int maxParams = Options.QueryParametersMaxCount ?? Dialect.QueryParametersMaxCount;

        if (ParserState.ParameterCount >= maxParams)
        {
            throw new SqlParameterLimitException(maxParams, ParserState.ParameterCount + 1);
        }

        int index = Options.ParameterIndexStart + ParserState.ParameterCount;
        string prefix = Options.ParameterPrefixOverride ?? Dialect.ParameterPrefix;
        string paramKey = $"{prefix}{index}";
        
        Parameters[paramKey] = value ?? DBNull.Value;
        ParserState.ParameterCount++;
        
        return paramKey;
    }

    /// <inheritdoc />
    public void Reset()
    {
        Parameters = new Dictionary<string, object?>();
        ParserState.Reset();
    }
}