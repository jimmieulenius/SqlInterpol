using SqlInterpol.Testing.Xunit;
using Xunit;

namespace SqlInterpol.Testing.Specifications;

public interface IJoinAsTestSuite : ISqlTestSuiteBase
{
    static abstract TheoryData<SqlTestCase> JoinWithLiteralAliasesData { get; }
    static abstract TheoryData<SqlTestCase> JoinWithExplicitApiAliasesData { get; }
    static abstract TheoryData<SqlTestCase> SelfJoinData { get; }
    static abstract TheoryData<SqlTestCase> JoinWithConfigOverrideData { get; }
}