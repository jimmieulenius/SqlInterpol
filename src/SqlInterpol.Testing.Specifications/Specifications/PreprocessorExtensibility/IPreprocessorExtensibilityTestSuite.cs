using SqlInterpol.Testing.Xunit;
using Xunit;

namespace SqlInterpol.Testing.Specifications;

public interface IPreprocessorExtensibilityTestSuite : ISqlTestSuiteBase
{
    static abstract TheoryData<SqlTestCase> CustomRuleData { get; }
}