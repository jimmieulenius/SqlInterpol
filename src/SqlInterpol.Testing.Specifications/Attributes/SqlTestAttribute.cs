namespace SqlInterpol.Testing.Specifications;

/// <summary>
/// Marks a method in a test-suite template class as the implementation target for a
/// data-driven SQL test. The source generator replaces this attribute with xUnit's
/// <c>[Theory]</c> and <c>[MemberData(nameof(<see cref="DataPropertyName"/>))]</c>.
/// </summary>
/// <param name="dataPropertyName">
/// The name of the static <c>TheoryData&lt;SqlTestCase&gt;</c> property on the implementing
/// class that supplies test rows for this method.
/// </param>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
public sealed class SqlTestAttribute(string dataPropertyName) : Attribute
{
    /// <summary>
    /// Gets the name of the <c>TheoryData</c> property that supplies test cases for this method.
    /// </summary>
    public string DataPropertyName { get; } = dataPropertyName;
}