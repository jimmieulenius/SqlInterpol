using SqlInterpol.Testing.Xunit;
using Xunit;

namespace SqlInterpol.Testing.Specifications;

public interface IWhereTestSuite : ISqlTestSuiteBase
{
    static abstract TheoryData<SqlTestCase> WhereSimpleParameterData { get; }
    static abstract TheoryData<SqlTestCase> WhereInCollectionData { get; }
}