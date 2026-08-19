using SqlInterpol.Testing.Xunit;
using Xunit;

namespace SqlInterpol.Testing.Specifications;

public interface IOptionsTestSuite : ISqlTestSuiteBase
{
    static abstract TheoryData<SqlTestCase> CustomParameterIndexStartData { get; }
    static abstract TheoryData<SqlTestCase> EnumFormattingData { get; }
}