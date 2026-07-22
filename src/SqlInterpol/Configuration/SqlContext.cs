using SqlInterpol.Pipeline;

namespace SqlInterpol.Configuration;

/// <summary>
/// Default implementation of <see cref="ISqlContext"/>, holding the runtime state for a
/// <see cref="SqlBuilder"/> query build: dialect, renderer, options, and parameters.
/// </summary>
/// <param name="builder">The <see cref="SqlBuilder"/> that owns this context.</param>
/// <param name="dialect">The active SQL dialect.</param>
/// <param name="renderer">The renderer used to convert segments to SQL strings.</param>
/// <param name="options">The configuration options for this context.</param>
public class SqlContext(SqlBuilder builder, ISqlDialect dialect, ISqlSegmentRenderer renderer, SqlInterpolOptions? options = null) : ISqlContext
{
    /// <summary>Gets the <see cref="SqlBuilder"/> that owns this context.</summary>
    public SqlBuilder Builder { get; } = builder;

    /// <summary>Gets the active SQL dialect.</summary>
    public ISqlDialect Dialect { get; } = dialect;

    /// <summary>Gets the renderer used to convert segments to SQL strings.</summary>
    public ISqlSegmentRenderer Renderer { get; } = renderer;

    /// <summary>Gets the configuration options for this context.</summary>
    public SqlInterpolOptions Options { get; } = options ?? new SqlInterpolOptions { Dialect = dialect.Kind };

    /// <summary>Gets the accumulated dictionary of named parameters extracted from interpolated values.</summary>
    public IDictionary<string, object?> Parameters { get; private set; } = new Dictionary<string, object?>();

    /// <inheritdoc />
    public string AddParameter(object? value)
    {
        int maxParams = Options.QueryParametersMaxCount ?? Dialect.QueryParametersMaxCount;
        int currentCount = Parameters.Count; // Pure, zero-allocation tracking via the collection itself!

        if (currentCount >= maxParams)
        {
            throw new SqlParameterLimitException(maxParams, currentCount + 1);
        }

        int index = Options.ParameterIndexStart + currentCount;
        string prefix = Options.ParameterPrefixOverride ?? Dialect.ParameterPrefix;
        string paramKey = $"{prefix}{index}";
        
        Parameters[paramKey] = value ?? DBNull.Value;
        
        return paramKey;
    }

    /// <inheritdoc />
    public void Reset()
    {
        Parameters = new Dictionary<string, object?>();
    }
}