namespace SqlInterpol.Testing.Generators;

/// <summary>
/// Well-known string constants used throughout the testing source generator pipeline
/// to avoid magic-string duplication and make refactoring safe.
/// </summary>
internal static class GeneratorConstants
{
    /// <summary>
    /// Diagnostic emitted when a <c>[SqlTest("X")]</c> attribute in a template class references
    /// a property name <c>X</c> that is not declared on the suite contract interface.
    /// </summary>
    public const string DiagnosticMismatchedTestDataMember = "SQLITG01";

    // Fully-qualified attribute names used by the test suite generator for attribute matching.
    /// <summary>Fully-qualified name of <c>SqlTestSuiteAttribute</c>.</summary>
    public const string SqlTestSuiteAttributeFullName = "SqlInterpol.Testing.Specifications.SqlTestSuiteAttribute";

    /// <summary>Fully-qualified name of <c>SqlTestAttribute</c>.</summary>
    public const string SqlTestAttributeFullName = "SqlInterpol.Testing.Specifications.SqlTestAttribute";
}
