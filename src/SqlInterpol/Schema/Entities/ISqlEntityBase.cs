using SqlInterpol.Segments;

namespace SqlInterpol.Schema;

/// <summary>
/// Provides internal structural properties for a mapped SQL entity.
/// </summary>
public interface ISqlEntityBase : ISqlFragment
{
    /// <summary>
    /// Gets the assigned execution role (e.g. Table or CTE) for this entity.
    /// </summary>
    SqlEntityRole Role { get; }

    /// <summary>
    /// Gets the fragment representing the entity's reference (e.g., its alias or table name).
    /// </summary>
    ISqlReference Reference { get; }
    
    /// <summary>
    /// Gets the fragment representing the entity's declaration (e.g., used in a FROM or JOIN clause).
    /// </summary>
    ISqlDeclaration Declaration { get; }

    /// <summary>
    /// Gets the underlying C# model type this entity represents.
    /// Provides O(1) type resolution for the rendering engine.
    /// </summary>
    Type ModelType { get; }
}

/// <summary>
/// Provides internal structural properties for a mapped SQL entity bound to a specific CLR model type.
/// </summary>
/// <typeparam name="T">The CLR model type.</typeparam>
public interface ISqlEntityBase<T> : ISqlEntityBase
{
}