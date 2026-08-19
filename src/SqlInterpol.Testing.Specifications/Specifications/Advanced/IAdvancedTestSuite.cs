using SqlInterpol.Testing.Xunit;
using Xunit;

namespace SqlInterpol.Testing.Specifications;

public interface IAdvancedTestSuite : ISqlTestSuiteBase
{
    static abstract TheoryData<SqlTestCase> DynamicQueryData { get; }
    static abstract TheoryData<SqlTestCase> AdvancedDynamicQueryData { get; }
    static abstract TheoryData<SqlTestCase> ComplexRawSqlData { get; }
}