using SqlInterpol.Testing.Xunit;
using Xunit;

namespace SqlInterpol.Testing.Specifications;

public interface IRawSqlTestSuite : ISqlTestSuiteBase
{
    static abstract TheoryData<SqlTestCase> ComplexRawSqlData { get; }
    static abstract TheoryData<SqlTestCase> WindowFunctionData { get; }
}