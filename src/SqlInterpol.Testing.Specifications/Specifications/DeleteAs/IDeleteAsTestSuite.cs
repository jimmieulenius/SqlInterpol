using SqlInterpol.Testing.Xunit;
using Xunit;

namespace SqlInterpol.Testing.Specifications;

public interface IDeleteAsTestSuite : ISqlTestSuiteBase
{
    static abstract TheoryData<SqlTestCase> DeleteWithExplicitAliasData { get; }
    static abstract TheoryData<SqlTestCase> DeleteWithoutAliasData { get; }
}