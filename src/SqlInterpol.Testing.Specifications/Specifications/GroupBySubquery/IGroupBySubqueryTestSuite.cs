using SqlInterpol.Testing.Xunit;
using Xunit;

namespace SqlInterpol.Testing.Specifications;

public interface IGroupBySubqueryTestSuite : ISqlTestSuiteBase
{
    static abstract TheoryData<SqlTestCase> GroupBySubqueryData { get; }
}