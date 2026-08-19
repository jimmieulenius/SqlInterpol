using SqlInterpol.Testing.Xunit;
using Xunit;

namespace SqlInterpol.Testing.Specifications;

public interface IRenderExtensionTestSuite : ISqlTestSuiteBase
{
    static abstract TheoryData<SqlTestCase> AsDeclarationData { get; }
    static abstract TheoryData<SqlTestCase> AsAliasData { get; }
    static abstract TheoryData<SqlTestCase> AsBaseData { get; }
    static abstract TheoryData<SqlTestCase> AsColumnData { get; }
    static abstract TheoryData<SqlTestCase> CombinedData { get; }
}