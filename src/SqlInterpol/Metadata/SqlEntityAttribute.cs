namespace SqlInterpol;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
public abstract class SqlEntityAttribute(string? name = null, string? schema = null) : Attribute
{
    public string? Name { get; } = name;
    public string? Schema { get; init; } = schema;
    public abstract SqlEntityType Type { get; }
}