using SqlInterpol.Testing.Xunit;
using Xunit;

namespace SqlInterpol.Testing.Specifications;

public interface ISelectAsTestSuite : ISqlTestSuiteBase
{
    static abstract TheoryData<SqlTestCase> ProjectionAsLiteralData { get; }
    static abstract TheoryData<SqlTestCase> RawColumnAsProjectionData { get; }
    static abstract TheoryData<SqlTestCase> ProjectionAsProjectionWithAttributeData { get; }
}