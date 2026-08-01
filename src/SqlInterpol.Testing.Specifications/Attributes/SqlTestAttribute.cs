namespace SqlInterpol.Testing.Specifications;

[AttributeUsage(AttributeTargets.Method)]
public class SqlTestAttribute(string dataPropertyName) : Attribute
{
    public string DataPropertyName { get; } = dataPropertyName;
}