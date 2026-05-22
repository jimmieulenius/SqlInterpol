namespace SqlInterpol;

/// <summary>
/// Represents the full declaration fragment for a SQL entity, rendering it with its alias
/// (e.g. <c>"Products" AS "p"</c> for use in a FROM clause).
/// </summary>
public interface ISqlDeclaration : ISqlFragment
{
    /// <summary>Gets the entity this declaration belongs to.</summary>
    ISqlEntityBase Entity { get; }
}