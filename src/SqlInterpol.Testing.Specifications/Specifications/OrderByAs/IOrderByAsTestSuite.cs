using SqlInterpol.Testing.Xunit;
using Xunit;

namespace SqlInterpol.Testing.Specifications;

public interface IOrderByAsTestSuite : ISqlTestSuiteBase
{
    static abstract TheoryData<SqlTestCase> OrderByWithExplicitAliasData { get; }
}