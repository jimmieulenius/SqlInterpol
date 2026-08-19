using SqlInterpol.Testing.Xunit;
using Xunit;

namespace SqlInterpol.Testing.Specifications;

public interface IWhereAsTestSuite : ISqlTestSuiteBase
{
    static abstract TheoryData<SqlTestCase> WhereWithAliasedEntityData { get; }
}