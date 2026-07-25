using SqlInterpol.Configuration;

namespace SqlInterpol.Segments;

/// <summary>
/// Implemented by SQL fragments that must eagerly materialize parameter placeholders
/// into the active context before the rendering pass begins.
/// </summary>
public interface ISqlParameterGenerator
{
    /// <summary>
    /// Flushes any pending parameter state into the context's parameter collection.
    /// </summary>
    /// <param name="context">The active <see cref="ISqlContext"/> holding the parameter collection to populate.</param>
    void GenerateParameters(ISqlContext context);
}