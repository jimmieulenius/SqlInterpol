using System;

namespace SqlInterpol.Schema;

/// <summary>
/// Specifies that a class is mapped to a specific database view.
/// </summary>
/// <param name="name">The physical name of the view in the database.</param>
/// <param name="schema">The optional schema the view belongs to.</param>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, Inherited = false)]
public class SqlViewAttribute(string name, string? schema = null) : Attribute
{
    /// <summary>Gets the physical view name.</summary>
    public string Name { get; } = name;
    
    /// <summary>Gets the optional database schema.</summary>
    public string? Schema { get; } = schema;
}