using SqlInterpol.Configuration;
using SqlInterpol.Testing.Specifications;
using SqlInterpol.Testing.Xunit;
using Xunit;

namespace SqlInterpol.Tests.Dialects.Oracle;

public abstract partial class OracleUpdateAsTestSuiteBase : IUpdateAsTestSuite
{
    public virtual SqlBuilder CreateBuilder(SqlInterpolOptions? options = null) => SqlBuilder.Oracle(options);

    public static TheoryData<SqlTestCase> UpdateSetWithAliasData => [new SqlTestCase(
        expectedExceptionType: typeof(SqlDialectException)
    )];
}