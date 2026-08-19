using SqlInterpol.Testing.Xunit;
using Xunit;

namespace SqlInterpol.Testing.Specifications;

public interface ICustomParserTestSuite : ISqlTestSuiteBase
{
    static abstract TheoryData<SqlTestCase> CustomParserData { get; }
}