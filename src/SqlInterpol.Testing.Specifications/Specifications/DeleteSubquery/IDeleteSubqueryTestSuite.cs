using SqlInterpol.Testing.Xunit;
using Xunit;

namespace SqlInterpol.Testing.Specifications;

public interface IDeleteSubqueryTestSuite : ISqlTestSuiteBase
{
    static abstract TheoryData<SqlTestCase> Delete_WithSubqueryData { get; }
}