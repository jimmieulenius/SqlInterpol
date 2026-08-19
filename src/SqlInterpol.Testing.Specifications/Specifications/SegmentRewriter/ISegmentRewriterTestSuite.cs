using SqlInterpol.Testing.Xunit;
using Xunit;

namespace SqlInterpol.Testing.Specifications;

public interface ISegmentRewriterTestSuite : ISqlTestSuiteBase
{
    static abstract TheoryData<SqlTestCase> SoftDeleteData { get; }
}