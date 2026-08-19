using SqlInterpol.Testing.Xunit;
using Xunit;

namespace SqlInterpol.Testing.Specifications;

public interface ISelectTestSuite : ISqlTestSuiteBase
{
    static abstract TheoryData<SqlTestCase> SelectExpansionData { get; }
    static abstract TheoryData<SqlTestCase> SingleColumnData { get; }
    static abstract TheoryData<SqlTestCase> MultipleColumnsData { get; }
    static abstract TheoryData<SqlTestCase> SqlFunctionData { get; }
    static abstract TheoryData<SqlTestCase> LiteralParameterData { get; }
    static abstract TheoryData<SqlTestCase> CustomColumnAttributeData { get; }
    static abstract TheoryData<SqlTestCase> SelectDistinctVerticalLayoutData { get; }
    static abstract TheoryData<SqlTestCase> TopKeywordData { get; }
    static abstract TheoryData<SqlTestCase> SelectComplexData { get; }
}