using System.Linq.Expressions;
using SqlInterpol.Parsing;

namespace SqlInterpol;

/// <summary>
/// Core contract for all SQL entity types. Provides column access, declaration, and reference members.
/// </summary>
/// <seealso cref="ISqlEntityBase{T}"/>
public interface ISqlEntityBase : ISqlFragment
{
    /// <summary>Gets the assigned execution role (e.g. Table or CTE) for this entity.</summary>
    SqlEntityRole Role { get; }

    /// <summary>
    /// Gets the fragment that renders this entity as a full declaration, including alias
    /// (e.g. <c>"Products" AS "p"</c>).
    /// </summary>
    ISqlDeclaration Declaration { get; }

    /// <summary>
    /// Gets the fragment used to reference this entity in query clauses (e.g. just <c>"p"</c>).
    /// </summary>
    ISqlReference Reference { get; }

    /// <summary>
    /// Gets the underlying C# model type this entity represents.
    /// Provides O(1) type resolution for the rendering engine.
    /// </summary>
    Type ModelType { get; }

    /// <summary>
    /// Gets a SQL reference to the named column on this entity.
    /// </summary>
    /// <param name="columnName">The physical column name.</param>
    /// <returns>An <see cref="ISqlReference"/> representing the qualified column (e.g. <c>"p"."Name"</c>).</returns>
    ISqlReference this[string columnName] { get; }

    /// <summary>
    /// Gets a raw SQL fragment for the named column, without alias qualification.
    /// </summary>
    /// <param name="name">The physical column name.</param>
    /// <returns>An <see cref="ISqlFragment"/> representing the bare column identifier.</returns>
    ISqlFragment Column(string name);
}

/// <summary>
/// Typed extension of <see cref="ISqlEntityBase"/> that adds expression-based column access.
/// </summary>
/// <typeparam name="T">The CLR type that maps to this SQL entity.</typeparam>
public interface ISqlEntityBase<T> : ISqlEntityBase
{
    // TODO (v2.0): Remove when deleting old lambda syntax. Use direct POCO property access instead.
    [Obsolete("Use the zero-allocation out var syntax and direct POCO property access (e.g., {p.Id}).")]
    ISqlReference this[Expression<Func<T, object>> propertySelector] { get; }
}