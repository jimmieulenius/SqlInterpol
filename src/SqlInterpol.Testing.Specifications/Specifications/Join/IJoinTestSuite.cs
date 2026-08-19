using SqlInterpol.Testing.Xunit;
using Xunit;

namespace SqlInterpol.Testing.Specifications;

public interface IJoinTestSuite : ISqlTestSuiteBase
{
    static abstract TheoryData<SqlTestCase> JoinTwoEntitiesData { get; }
}