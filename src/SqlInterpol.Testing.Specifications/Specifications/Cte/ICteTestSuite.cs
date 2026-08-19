using SqlInterpol.Testing.Xunit;
using Xunit;

namespace SqlInterpol.Testing.Specifications;

public interface ICteTestSuite : ISqlTestSuiteBase
{
    static abstract TheoryData<SqlTestCase> Select_WithCteData { get; }
    static abstract TheoryData<SqlTestCase> Select_WithRecursiveCteData { get; }
    static abstract TheoryData<SqlTestCase> RawCteData { get; }
}