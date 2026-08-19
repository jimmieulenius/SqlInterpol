using SqlInterpol.Testing.Xunit;
using Xunit;

namespace SqlInterpol.Testing.Specifications;

public interface IDeleteTestSuite : ISqlTestSuiteBase
{
    static abstract TheoryData<SqlTestCase> DeletePureManualData { get; }
    static abstract TheoryData<SqlTestCase> DeleteMultiTableData { get; }
    static abstract TheoryData<SqlTestCase> DeleteTemplateData { get; }
}