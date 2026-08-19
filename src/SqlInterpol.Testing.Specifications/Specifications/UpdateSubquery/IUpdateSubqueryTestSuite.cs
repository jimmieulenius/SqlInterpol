using SqlInterpol.Testing.Xunit;
using Xunit;

namespace SqlInterpol.Testing.Specifications;

public interface IUpdateSubqueryTestSuite : ISqlTestSuiteBase
{
    static abstract TheoryData<SqlTestCase> UpdateSubqueryData { get; }
    static abstract TheoryData<SqlTestCase> UpdateTypeSafeSubqueryData { get; }
}