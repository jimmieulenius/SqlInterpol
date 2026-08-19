using SqlInterpol.Testing.Xunit;
using Xunit;

namespace SqlInterpol.Testing.Specifications;

public interface IJoinSubqueryTestSuite : ISqlTestSuiteBase
{
    static abstract TheoryData<SqlTestCase> JoinSubqueryData { get; }
}