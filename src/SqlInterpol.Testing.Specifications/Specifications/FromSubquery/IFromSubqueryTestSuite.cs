using SqlInterpol.Testing.Xunit;
using Xunit;

namespace SqlInterpol.Testing.Specifications;

public interface IFromSubqueryTestSuite : ISqlTestSuiteBase
{
    static abstract TheoryData<SqlTestCase> From_SubqueryData { get; }
}