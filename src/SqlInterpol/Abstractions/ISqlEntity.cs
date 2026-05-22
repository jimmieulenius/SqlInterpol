namespace SqlInterpol;

/// <summary>
/// Represents a named SQL entity (table or view) with optional schema qualification.
/// </summary>
/// <seealso cref="ISqlEntity{T}"/>
public interface ISqlEntity : ISqlEntityBase
{
    /// <summary>Gets the physical table or view name.</summary>
    string Name { get; }

    /// <summary>Gets the schema that owns this entity, or <see langword="null"/> for the default schema.</summary>
    string? Schema { get; }
}

/// <summary>
/// A typed <see cref="ISqlEntity"/> bound to a specific CLR type <typeparamref name="T"/>.
/// </summary>
/// <typeparam name="T">The CLR type that maps to this SQL entity.</typeparam>
public interface ISqlEntity<T> : ISqlEntity, ISqlEntityBase<T>
{
}