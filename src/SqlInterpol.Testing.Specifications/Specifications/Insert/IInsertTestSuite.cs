using SqlInterpol.Testing.Xunit;
using Xunit;

namespace SqlInterpol.Testing.Specifications;

public interface IInsertTestSuite : ISqlTestSuiteBase
{
    static abstract TheoryData<SqlTestCase> InsertData { get; }
    static abstract TheoryData<SqlTestCase> ManualInsertData { get; }
    static abstract TheoryData<SqlTestCase> BulkInsertData { get; }
    static abstract TheoryData<SqlTestCase> ReturningSingleData { get; }
    static abstract TheoryData<SqlTestCase> ReturningMultipleData { get; }
    static abstract TheoryData<SqlTestCase> InsertWithIgnoreData { get; }
    static abstract TheoryData<SqlTestCase> InsertComplexData { get; }
    static abstract TheoryData<SqlTestCase> InsertTemplateData { get; }
}