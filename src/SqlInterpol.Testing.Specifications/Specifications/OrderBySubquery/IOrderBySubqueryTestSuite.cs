using SqlInterpol.Testing.Xunit;
using Xunit;

namespace SqlInterpol.Testing.Specifications;

public interface IOrderBySubqueryTestSuite : ISqlTestSuiteBase
{
    static abstract TheoryData<SqlTestCase> OrderBySubqueryData { get; }
}