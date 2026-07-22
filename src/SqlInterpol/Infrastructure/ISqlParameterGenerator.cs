using SqlInterpol.Configuration;

namespace SqlInterpol.Infrastructure;

/// <summary>
/// Generates SQL parameters from the current context state into the active parameter collection.
/// </summary>
public interface ISqlParameterGenerator
{
    /// <summary>
    /// Flushes any pending parameter state into the context's parameter collection.
    /// </summary>
    /// <param name="context">The active <see cref="ISqlContext"/> holding the parameter collection to populate.</param>
    void GenerateParameters(ISqlContext context);
}