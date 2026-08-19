using SqlInterpol.Testing.Xunit;
using Xunit;

namespace SqlInterpol.Testing.Specifications;

public interface IGroupByTestSuite : ISqlTestSuiteBase
{
    static abstract TheoryData<SqlTestCase> GroupByCombinerData { get; }
    static abstract TheoryData<SqlTestCase> GroupByWithSqlRawData { get; }
    static abstract TheoryData<SqlTestCase> GroupByMixingTypedAndRawData { get; }
}