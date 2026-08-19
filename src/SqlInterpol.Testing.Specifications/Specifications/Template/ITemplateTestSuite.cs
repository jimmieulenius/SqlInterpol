using SqlInterpol.Testing.Xunit;
using Xunit;

namespace SqlInterpol.Testing.Specifications;

public interface ITemplateTestSuite : ISqlTestSuiteBase
{
    static abstract TheoryData<SqlTestCase> TemplateSelectData { get; }
    static abstract TheoryData<SqlTestCase> TemplateBulkInsertData { get; }
    static abstract TheoryData<SqlTestCase> TemplateUpdateData { get; }
    static abstract TheoryData<SqlTestCase> TemplateDeleteData { get; }
    static abstract TheoryData<SqlTestCase> TemplateManualInsertData { get; }
    static abstract TheoryData<SqlTestCase> TemplateManualUpdateData { get; }
}