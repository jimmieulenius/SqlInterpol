namespace SqlInterpol.Testing.Specifications;

[AttributeUsage(AttributeTargets.Interface, Inherited = false, AllowMultiple = false)]
public sealed class SqlTestSuiteAttribute(Type specificationType) : Attribute
{
    public Type SpecificationType { get; } = specificationType;
}