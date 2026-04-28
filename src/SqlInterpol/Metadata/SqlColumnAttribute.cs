namespace SqlInterpol.Metadata;

[AttributeUsage(AttributeTargets.Property)]
public class SqlColumnAttribute : Attribute
{
    public string? Name { get; }

    public SqlColumnAttribute()
    {
    }

    public SqlColumnAttribute(string name)
    {
        Name = name;
    }
}