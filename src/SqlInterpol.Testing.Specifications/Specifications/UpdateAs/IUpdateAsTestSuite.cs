using SqlInterpol.Testing.Xunit;
using Xunit;

namespace SqlInterpol.Testing.Specifications;

public interface IUpdateAsTestSuite : ISqlTestSuiteBase
{
    static abstract TheoryData<SqlTestCase> UpdateSetWithAliasData { get; }
}