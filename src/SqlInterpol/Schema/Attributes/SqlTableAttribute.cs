namespace SqlInterpol.Schema;

/// <summary>
/// Specifies that a class is mapped to a specific database table.
/// </summary>
/// <param name="name">The physical name of the table in the database.</param>
/// <param name="schema">The optional schema the table belongs to.</param>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, Inherited = false)]
public class SqlTableAttribute(string? name = null, string? schema = null) : Attribute
{
    /// <summary>Gets the physical table name.</summary>
    public string? Name { get; } = name;
    
    /// <summary>Gets the optional database schema.</summary>
    public string? Schema { get; } = schema;
}