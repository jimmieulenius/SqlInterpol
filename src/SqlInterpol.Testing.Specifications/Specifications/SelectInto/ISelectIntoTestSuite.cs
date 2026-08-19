using SqlInterpol.Testing.Xunit;
using Xunit;

namespace SqlInterpol.Testing.Specifications;

public interface ISelectIntoTestSuite : ISqlTestSuiteBase
{
    static abstract TheoryData<SqlTestCase> SelectIntoData { get; }
    static abstract TheoryData<SqlTestCase> SelectIntoParameterizedData { get; }
}