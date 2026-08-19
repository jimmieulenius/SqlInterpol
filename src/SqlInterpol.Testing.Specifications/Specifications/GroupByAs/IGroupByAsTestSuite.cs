using SqlInterpol.Testing.Xunit;
using Xunit;

namespace SqlInterpol.Testing.Specifications;

public interface IGroupByAsTestSuite : ISqlTestSuiteBase
{
    static abstract TheoryData<SqlTestCase> GroupByWithExplicitAliasData { get; }
}