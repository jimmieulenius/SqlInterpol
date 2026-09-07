using SqlInterpol.Testing.Xunit;
using Xunit;

namespace SqlInterpol.Testing.Specifications;

public interface IStoredProcedureTranspilationTestSuite : ISqlTestSuiteBase
{
    static abstract TheoryData<SqlTestCase> CallData { get; }
}
