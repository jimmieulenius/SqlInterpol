namespace SqlInterpol.Metadata;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
public abstract class SqlEntityAttribute(string name, string? schema = null) : Attribute
{
    public string Name { get; } = name;
    public string? Schema { get; init; } = schema;
    public abstract SqlEntityType Type { get; }
}