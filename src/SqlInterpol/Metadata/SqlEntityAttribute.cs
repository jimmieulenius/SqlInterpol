namespace SqlInterpol;

/// <summary>
/// Base class for entity-mapping attributes. Apply <see cref="SqlTableAttribute"/> or
/// <see cref="SqlViewAttribute"/> to a class to map it to a SQL entity.
/// </summary>
/// <param name="name">Optional override for the physical table or view name. When <see langword="null"/>, the class name is used.</param>
/// <param name="schema">Optional schema override. When <see langword="null"/>, the default schema is used.</param>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
public abstract class SqlEntityAttribute(string? name = null, string? schema = null) : Attribute
{
    /// <summary>Gets the physical name override, or <see langword="null"/> to use the class name.</summary>
    public string? Name { get; } = name;

    /// <summary>Gets the schema override, or <see langword="null"/> to use the default schema.</summary>
    public string? Schema { get; init; } = schema;

    /// <summary>Gets the entity kind (table, view, or subquery).</summary>
    public abstract SqlEntityType Type { get; }
}