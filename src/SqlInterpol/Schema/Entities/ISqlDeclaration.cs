using SqlInterpol.Segments;

namespace SqlInterpol.Schema;

/// <summary>
/// Represents the declaration fragment of an entity, typically used in a <c>FROM</c> 
/// or <c>JOIN</c> clause where the entity and its alias must be fully declared.
/// </summary>
public interface ISqlDeclaration : ISqlFragment
{
    /// <summary>
    /// Gets the base entity this declaration wraps.
    /// </summary>
    ISqlEntityBase Entity { get; }
}