using SqlInterpol.Testing.Xunit;
using Xunit;

namespace SqlInterpol.Testing.Specifications;

public interface IUpdateTestSuite : ISqlTestSuiteBase
{
    static abstract TheoryData<SqlTestCase> UpdateData { get; }
    static abstract TheoryData<SqlTestCase> UpdateExplicitData { get; }
    static abstract TheoryData<SqlTestCase> UpdateWithIgnoreData { get; }
    static abstract TheoryData<SqlTestCase> UpdateInvalidEntityData { get; }
    static abstract TheoryData<SqlTestCase> UpdateInvalidEntityPropertyData { get; }
    static abstract TheoryData<SqlTestCase> MultiTableUpdateData { get; }
    static abstract TheoryData<SqlTestCase> UpdateTemplateData { get; }
}