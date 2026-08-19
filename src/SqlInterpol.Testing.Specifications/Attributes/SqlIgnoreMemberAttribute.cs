namespace SqlInterpol.Testing.Specifications;

/// <summary>
/// Instructs the SqlTestSuiteGenerator to completely ignore this element when
/// copying members from the template into the dialect-specific implementations.
/// </summary>
[AttributeUsage(AttributeTargets.All, Inherited = false, AllowMultiple = false)]
public sealed class SqlGeneratorIgnoreAttribute : Attribute
{
}