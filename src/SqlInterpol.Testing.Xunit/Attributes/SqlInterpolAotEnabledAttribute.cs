namespace SqlInterpol.Testing.Xunit;

/// <summary>
/// Indicates that the test assembly was compiled with AOT testing enabled.
/// Injected automatically via MSBuild targets when AOT_ENABLED is defined in the test project.
/// </summary>
[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = false, Inherited = false)]
public sealed class SqlInterpolAotEnabledAttribute : Attribute
{
}