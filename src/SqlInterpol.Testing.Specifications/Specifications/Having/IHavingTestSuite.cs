using SqlInterpol.Testing.Xunit;
using Xunit;

namespace SqlInterpol.Testing.Specifications;

public interface IHavingTestSuite : ISqlTestSuiteBase
{
    static abstract TheoryData<SqlTestCase> SelectGroupByAndHavingData { get; }
}