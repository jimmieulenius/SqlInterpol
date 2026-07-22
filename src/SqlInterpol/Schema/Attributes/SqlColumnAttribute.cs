namespace SqlInterpol.Schema;

/// <summary>
/// Specifies the physical database column name that a property maps to.
/// </summary>
/// <param name="name">The physical column name.</param>
[AttributeUsage(AttributeTargets.Property, Inherited = false)]
public class SqlColumnAttribute(string name) : Attribute
{
    /// <summary>Gets the physical column name.</summary>
    public string Name { get; } = name;
}