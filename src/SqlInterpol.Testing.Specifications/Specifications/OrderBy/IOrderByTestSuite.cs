using SqlInterpol.Testing.Xunit;
using Xunit;

namespace SqlInterpol.Testing.Specifications;

public interface IOrderByTestSuite : ISqlTestSuiteBase
{
    static abstract TheoryData<SqlTestCase> OrderByExpressionData { get; }
    static abstract TheoryData<SqlTestCase> OrderByCombinerData { get; }
    static abstract TheoryData<SqlTestCase> OrderByEnumerableData { get; }
    static abstract TheoryData<SqlTestCase> OrderByRawData { get; }
    static abstract TheoryData<SqlTestCase> OrderByMixedRawData { get; }
    static abstract TheoryData<SqlTestCase> OrderByErrorData { get; }
}