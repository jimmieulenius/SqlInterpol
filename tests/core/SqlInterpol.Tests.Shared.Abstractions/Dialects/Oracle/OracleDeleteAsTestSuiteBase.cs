using SqlInterpol.Configuration;
using SqlInterpol.Testing.Specifications;
using SqlInterpol.Testing.Xunit;
using Xunit;

namespace SqlInterpol.Tests.Dialects.Oracle;

public abstract partial class OracleDeleteAsTestSuiteBase : IDeleteAsTestSuite
{
    private static readonly object[] _expectedParameters = [42];

    public virtual SqlBuilder CreateBuilder(SqlInterpolOptions? options = null) => SqlBuilder.Oracle(options);

    public static TheoryData<SqlTestCase> DeleteWithExplicitAliasData =>
    [
        new SqlTestCase(expectedExceptionType: typeof(SqlDialectException))
    ];

    public static TheoryData<SqlTestCase> DeleteWithoutAliasData =>
    [
        new SqlTestCase(
            expectedSql: [
                """
                DELETE FROM "dbo"."Orders"
                WHERE "dbo"."Orders"."Id" = :0
                """
            ],
            expectedParameters: _expectedParameters
        )
    ];
}