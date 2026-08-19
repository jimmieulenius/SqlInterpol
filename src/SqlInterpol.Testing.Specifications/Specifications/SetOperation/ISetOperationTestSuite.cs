using SqlInterpol.Testing.Xunit;
using Xunit;

namespace SqlInterpol.Testing.Specifications;

public interface ISetOperationTestSuite : ISqlTestSuiteBase
{
    static abstract TheoryData<SqlTestCase> QueryIntersectData { get; }
    static abstract TheoryData<SqlTestCase> QueryExceptData { get; }
    static abstract TheoryData<SqlTestCase> Select_UnionAllData { get; }
}