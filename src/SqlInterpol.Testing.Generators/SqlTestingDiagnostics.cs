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
    /// Emitted when a <c>[SqlTest("X")]</c> attribute on a template method
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

    /// <summary>
    /// Emitted when an interface mapped via <c>[SqlTestSuite(typeof(IContract))]</c> 
    /// does not inherit from <c>ISqlTestSuiteBase</c>.
    /// </summary>
    public static readonly DiagnosticDescriptor InvalidSuiteContractDiagnostic = new(
        id: GeneratorConstants.DiagnosticInvalidSuiteContract,
        title: "Invalid Test Suite Contract",
        messageFormat: "The interface '{0}' must inherit from ISqlTestSuiteBase to be used as a test suite contract.",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "Emitted when a class uses a [SqlTestSuite(typeof(IContract))] attribute but the specified contract does not implement ISqlTestSuiteBase."
    );

    /// <summary>
    /// Emitted when a target class implements a suite contract interface (e.g., <c>ILockTestSuite</c>), 
    /// but the source generator cannot find a corresponding abstract template class 
    /// annotated with <c>[SqlTestSuite(typeof(ILockTestSuite))]</c>.
    /// </summary>
    public static readonly DiagnosticDescriptor MissingTemplateDiagnostic = new(
        id: GeneratorConstants.DiagnosticMissingTemplate,
        title: "Missing Template for Suite Contract",
        messageFormat: "The class '{0}' implements suite contract '{1}', but no template class annotated with [SqlTestSuite(typeof({1}))] could be found in the Specifications. Did you forget the attribute or make a typo? Make sure the interface used in the attribute inherits from 'ISqlTestSuiteBase'.",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "Emitted to prevent silent failures when a dialect author implements a test contract, but the Source Generator cannot find the corresponding template implementation."
    );
}