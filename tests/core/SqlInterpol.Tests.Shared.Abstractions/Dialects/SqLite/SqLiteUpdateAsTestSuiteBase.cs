using SqlInterpol.Configuration;
using SqlInterpol.Testing.Specifications;
using SqlInterpol.Testing.Xunit;
using Xunit;

namespace SqlInterpol.Tests.Dialects.SqLite;

public abstract partial class SqLiteUpdateAsTestSuiteBase : IUpdateAsTestSuite
{
    public virtual SqlBuilder CreateBuilder(SqlInterpolOptions? options = null) => SqlBuilder.SqLite(options);

    public static TheoryData<SqlTestCase> UpdateSetWithAliasData => [new SqlTestCase(
        expectedExceptionType: typeof(SqlDialectException)
    )];
}