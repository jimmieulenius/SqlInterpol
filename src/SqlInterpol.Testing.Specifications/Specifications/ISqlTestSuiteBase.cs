namespace SqlInterpol.Testing.Specifications;

/// <summary>
/// The root contract for all SQL test suites. Implementing classes must provide a
/// <see cref="CreateBuilder"/> factory method that returns a <see cref="SqlBuilder"/>
/// configured for the dialect under test.
/// </summary>
/// <remarks>
/// This interface is the shared base for every dialect-specific suite interface
/// (e.g. <c>ILockTestSuite</c>). The source generator reads it via the
/// <c>[SqlTestSuite]</c> attribute on the derived interface to wire up
/// <see cref="SqlBuilder"/> instantiation in the emitted partial class.
/// </remarks>
public interface ISqlTestSuiteBase
{
    /// <summary>
    /// Creates a fresh <see cref="SqlBuilder"/> for the dialect under test.
    /// Called once per test method invocation by the generated partial class.
    /// </summary>
    /// <returns>A configured <see cref="SqlBuilder"/> ready for SQL generation.</returns>
    SqlBuilder CreateBuilder();
}