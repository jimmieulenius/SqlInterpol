using SqlInterpol.Testing.Xunit;
using Xunit;

namespace SqlInterpol.Testing.Specifications;

public interface IWindowFunctionTestSuite : ISqlTestSuiteBase
{
    static abstract TheoryData<SqlTestCase> WindowFunctionData { get; }
    static abstract TheoryData<SqlTestCase> RawWindowFunctionData { get; }
}