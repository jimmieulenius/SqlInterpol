namespace SqlInterpol.Configuration;

/// <summary>
/// Holds the runtime state for an active SQL build: the active dialect, configuration options,
/// and the accumulated parameter dictionary.
/// </summary>
/// <seealso cref="SqlContext"/>
public interface ISqlContext
{
    /// <summary>Gets the active SQL dialect that controls identifier quoting, parameter formatting, and feature support.</summary>
    ISqlDialect Dialect { get; }

    /// <summary>Gets the configuration options for this builder context.</summary>
    SqlInterpolOptions Options { get; }

    /// <summary>Gets the accumulated dictionary of named parameters extracted from interpolated values.</summary>
    IDictionary<string, object?> Parameters { get; }

    /// <summary>
    /// Registers an interpolated value as a named parameter and returns its generated parameter key.
    /// </summary>
    /// <param name="value">The value to register. <see langword="null"/> is stored as <see cref="DBNull.Value"/>.</param>
    /// <returns>The generated parameter key (e.g. <c>@p0</c>), ready for embedding in the SQL string.</returns>
    string AddParameter(object? value);

    /// <summary>
    /// Clears the parameter dictionary and resets internal parser state, preparing the context for the next query.
    /// </summary>
    void Reset();
}