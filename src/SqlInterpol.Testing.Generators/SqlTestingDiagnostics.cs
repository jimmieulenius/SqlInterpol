using Microsoft.CodeAnalysis;

namespace SqlInterpol.Testing.Generators;

/// <summary>
/// Central registry of all <see cref="DiagnosticDescriptor"/> instances emitted by
/// SqlInterpol's source generators. Keeping every descriptor here ensures that diagnostic
/// IDs, messages, and severities are defined in exactly one place.
/// </summary>
public static class SqlTestingDiagnostics
{
    /// <summary>
    /// <c>SQLIG11</c> — emitted when a <c>[SqlTest("X")]</c> attribute on a template method
    /// references a data property name <c>X</c> that is not declared as a <c>static abstract</c>
    /// member on the suite contract interface. Without a matching interface member the generator
    /// cannot emit a valid <c>[MemberData(nameof(X))]</c>, so the test method is silently skipped.
    /// </summary>
    public static readonly DiagnosticDescriptor MismatchedTestDataMemberDiagnostic = new(
        id: GeneratorConstants.DiagnosticMismatchedTestDataMember,
        title: "Mismatched SqlTest Data Property",
        messageFormat: "[SqlTest(\"{0}\")] on template method '{1}' in '{2}' references a property that does not exist on interface '{3}'. Add 'static abstract TheoryData<SqlTestCase> {0} {{ get; }}' to the interface, or correct the property name in [SqlTest].",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "Emitted when a [SqlTest] attribute string does not match any static abstract member declared on the suite contract interface, which would cause the test method to be silently dropped from the generated output."
    );
}