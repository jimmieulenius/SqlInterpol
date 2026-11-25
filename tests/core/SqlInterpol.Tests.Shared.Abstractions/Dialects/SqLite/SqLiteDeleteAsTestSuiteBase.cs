using SqlInterpol.Configuration;
using SqlInterpol.Testing.Specifications;
using SqlInterpol.Testing.Xunit;
using Xunit;

namespace SqlInterpol.Tests.Dialects.SqLite;

public abstract partial class SqLiteDeleteAsTestSuiteBase : IDeleteAsTestSuite
{
    private static readonly object[] _expectedParameters = [42];

    public virtual SqlBuilder CreateBuilder(SqlInterpolOptions? options = null) => SqlBuilder.SqLite(options);

    public static TheoryData<SqlTestCase> DeleteWithExplicitAliasData =>
    [
        new SqlTestCase(expectedExceptionType: typeof(SqlDialectException))
    ];

    public static TheoryData<SqlTestCase> DeleteWithoutAliasData =>
    [
        new SqlTestCase(
            expectedSql: [
                """
                DELETE FROM "Orders"
                WHERE "Orders"."Id" = @p1
                """
            ],
            expectedParameters: _expectedParameters
        )
    ];
}