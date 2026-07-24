namespace SqlInterpol.Schema;

/// <summary>
/// An internal registry used by the builder to map initialized entity variables 
/// to their schema configuration and scope contexts.
/// </summary>
public interface ISqlEntityRegistry
{
    /// <summary>
    /// Registers a new entity into the builder's scope.
    /// </summary>
    /// <typeparam name="T">The CLR model type of the entity.</typeparam>
    /// <param name="name">An optional explicit table or view name.</param>
    /// <param name="schema">An optional explicit schema name.</param>
    /// <param name="alias">An optional explicit alias.</param>
    /// <returns>The registered SQL entity instance.</returns>
    ISqlEntityBase<T> RegisterEntity<T>(string? name = null, string? schema = null, string? alias = null);
}