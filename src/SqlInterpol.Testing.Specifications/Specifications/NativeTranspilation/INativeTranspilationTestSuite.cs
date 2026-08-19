using SqlInterpol.Testing.Xunit;
using Xunit;

namespace SqlInterpol.Testing.Specifications;

public interface INativeTranspilationTestSuite : ISqlTestSuiteBase
{
    static abstract TheoryData<SqlTestCase> BooleanTranspilationData { get; }
    static abstract TheoryData<SqlTestCase> BooleanBoundaryData { get; }
    static abstract TheoryData<SqlTestCase> ConcatOperatorData { get; }
    static abstract TheoryData<SqlTestCase> StringSafetyData { get; }
}