using SqlInterpol.Testing.Xunit;
using Xunit;

namespace SqlInterpol.Testing.Specifications;

public interface IWhereSubqueryTestSuite : ISqlTestSuiteBase
{
    static abstract TheoryData<SqlTestCase> WhereInSubqueryData { get; }
}