using SqlInterpol.Testing.Xunit;
using Xunit;

namespace SqlInterpol.Testing.Specifications;

public interface IFromAsTestSuite : ISqlTestSuiteBase
{
    static abstract TheoryData<SqlTestCase> From_EntityManualAliasData { get; }
    static abstract TheoryData<SqlTestCase> From_EntitySqlTableAttributeData { get; }
    static abstract TheoryData<SqlTestCase> From_LiteralTableAsEntityWithoutAttributeData { get; }
    static abstract TheoryData<SqlTestCase> From_LiteralTableAsExplicitAliasedEntityData { get; }
    static abstract TheoryData<SqlTestCase> From_EntityAsEntityWithSchemaData { get; }
    static abstract TheoryData<SqlTestCase> FromAsEntityAsItsOwnAliasInceptionData { get; }
    static abstract TheoryData<SqlTestCase> From_EntityAutoAliasingData { get; }
    static abstract TheoryData<SqlTestCase> From_EntityAutoAliasingManualOverrideData { get; }
    static abstract TheoryData<SqlTestCase> From_AutoAliasingInceptionData { get; }
}