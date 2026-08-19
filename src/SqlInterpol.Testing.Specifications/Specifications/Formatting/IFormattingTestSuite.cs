using SqlInterpol.Testing.Xunit;
using Xunit;

namespace SqlInterpol.Testing.Specifications;

public interface IFormattingTestSuite : ISqlTestSuiteBase
{
    static abstract TheoryData<SqlTestCase> Select_WithNewLinesData { get; }
    static abstract TheoryData<SqlTestCase> Select_WithTabsData { get; }
    static abstract TheoryData<SqlTestCase> Select_WithExtraSpacesData { get; }
    static abstract TheoryData<SqlTestCase> Select_WithMixedWhitespaceData { get; }
    static abstract TheoryData<SqlTestCase> Select_WithCommentsData { get; }
    static abstract TheoryData<SqlTestCase> InsertVerticalLayoutData { get; }
    static abstract TheoryData<SqlTestCase> UpdateVerticalLayoutData { get; }
    static abstract TheoryData<SqlTestCase> BulkInsertVerticalLayoutData { get; }
    static abstract TheoryData<SqlTestCase> WhereInVerticalLayoutData { get; }
    static abstract TheoryData<SqlTestCase> OrderByEnumerableVerticalLayoutData { get; }
    static abstract TheoryData<SqlTestCase> SelectEntityExpansionVerticalLayoutData { get; }
}