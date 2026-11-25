using SqlInterpol.Configuration;
using SqlInterpol.Testing.Specifications;
using SqlInterpol.Testing.Xunit;
using Xunit;

namespace SqlInterpol.Tests.Dialects.Firebird;

public abstract partial class FirebirdUpdateAsTestSuiteBase : IUpdateAsTestSuite
{
    public virtual SqlBuilder CreateBuilder(SqlInterpolOptions? options = null) => SqlBuilder.Firebird(options);

    public static TheoryData<SqlTestCase> UpdateSetWithAliasData => [new SqlTestCase(
        expectedExceptionType: typeof(SqlDialectException)
    )];
}