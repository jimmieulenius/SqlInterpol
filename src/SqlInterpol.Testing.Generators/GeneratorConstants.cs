namespace SqlInterpol.Testing.Generators;

/// <summary>
/// Well-known string constants used throughout the testing source generator pipeline
/// to avoid magic-string duplication and make refactoring safe.
/// </summary>
internal static class GeneratorConstants
{
    /// <summary>
    /// Diagnostic ID emitted when a <c>[SqlTest("X")]</c> attribute references
    /// a property name <c>X</c> that is not declared on the suite contract interface.
    /// </summary>
    public const string DiagnosticMismatchedTestDataMember = "SQLITG01";

    /// <summary>
    /// Diagnostic ID emitted when a mapped interface does not inherit from <c>ISqlTestSuiteBase</c>.
    /// </summary>
    public const string DiagnosticInvalidSuiteContract = "SQLITG02";
    
    /// <summary>
    /// Diagnostic ID emitted when a dialect class implements a contract interface, 
    /// but no corresponding template file could be found.
    /// </summary>
    public const string DiagnosticMissingTemplate = "SQLITG03";

    /// <summary>
    /// The short name of the base interface that all suite contracts must inherit from.
    /// </summary>
    public const string SqlTestSuiteBaseInterfaceName = "ISqlTestSuiteBase";

    /// <summary>
    /// Fully-qualified name of <c>SqlTestSuiteAttribute</c> used for semantic matching.
    /// </summary>
    public const string SqlTestSuiteAttributeFullName = "SqlInterpol.Testing.Specifications.SqlTestSuiteAttribute";

    /// <summary>
    /// Fully-qualified name of <c>SqlTestAttribute</c> used for semantic matching.
    /// </summary>
    public const string SqlTestAttributeFullName = "SqlInterpol.Testing.Specifications.SqlTestAttribute";
}