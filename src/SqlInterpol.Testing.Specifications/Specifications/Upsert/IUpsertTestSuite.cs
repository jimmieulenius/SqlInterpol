using SqlInterpol.Testing.Xunit;
using Xunit;

namespace SqlInterpol.Testing.Specifications;

public interface IUpsertTestSuite : ISqlTestSuiteBase
{
    static abstract TheoryData<SqlTestCase> UpsertData { get; }
    static abstract TheoryData<SqlTestCase> OnDuplicateKeyData { get; }
    static abstract TheoryData<SqlTestCase> OnConflictData { get; }
    static abstract TheoryData<SqlTestCase> UpsertTemplateData { get; }
}