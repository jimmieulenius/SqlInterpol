using SqlInterpol.Testing.Xunit;
using Xunit;

namespace SqlInterpol.Testing.Specifications;

public interface IPagingTestSuite : ISqlTestSuiteBase
{
    static abstract TheoryData<SqlTestCase> Paging_WithImplicitLimitOffsetData { get; }
}