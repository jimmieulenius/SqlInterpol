namespace SqlInterpol.Metadata;

[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public class SqlTableAttribute : Attribute
{
    public string? Schema { get; set; }
    public string? Name { get; set; }

    public SqlTableAttribute(string? name = null, string? schema = null)
    {
        Name = name;
        Schema = schema;
    }
}