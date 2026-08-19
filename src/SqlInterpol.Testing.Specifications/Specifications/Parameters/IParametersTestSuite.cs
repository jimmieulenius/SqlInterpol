using SqlInterpol.Testing.Xunit;
using Xunit;

namespace SqlInterpol.Testing.Specifications;

public interface IParametersTestSuite : ISqlTestSuiteBase
{
    static abstract TheoryData<SqlTestCase> SingleParameterData { get; }
    static abstract TheoryData<SqlTestCase> NullParameterData { get; }
    static abstract TheoryData<SqlTestCase> CollectionParameterData { get; }
    static abstract TheoryData<SqlTestCase> ExplicitSqlArgData { get; }
}