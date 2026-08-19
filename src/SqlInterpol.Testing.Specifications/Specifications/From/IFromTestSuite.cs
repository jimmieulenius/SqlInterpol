using SqlInterpol.Testing.Xunit;
using Xunit;

namespace SqlInterpol.Testing.Specifications;

public interface IFromTestSuite : ISqlTestSuiteBase
{
    static abstract TheoryData<SqlTestCase> From_SingleEntityData { get; }
    static abstract TheoryData<SqlTestCase> From_EntityWithSqlTableNameOnlyData { get; }
    static abstract TheoryData<SqlTestCase> From_Entity_WithSqlTableNameAndSchemaData { get; }
}