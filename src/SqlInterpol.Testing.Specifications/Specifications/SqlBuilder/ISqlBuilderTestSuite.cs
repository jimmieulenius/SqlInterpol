using SqlInterpol.Testing.Xunit;
using Xunit;

namespace SqlInterpol.Testing.Specifications;

public interface ISqlBuilderTestSuite : ISqlTestSuiteBase
{
    static abstract TheoryData<SqlTestCase> AppendData { get; }
    static abstract TheoryData<SqlTestCase> AppendLineData { get; }
    static abstract TheoryData<SqlTestCase> RawStringData { get; }
}