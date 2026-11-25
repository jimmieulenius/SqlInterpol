using SqlInterpol.Configuration;
using SqlInterpol.Testing.Specifications;
using SqlInterpol.Testing.Xunit;
using Xunit;

namespace SqlInterpol.Tests.Dialects.PostgreSql;

public abstract partial class PostgreSqlDeleteAsTestSuiteBase : IDeleteAsTestSuite
{
    private static readonly object[] _expectedParameters = [42];

    public virtual SqlBuilder CreateBuilder(SqlInterpolOptions? options = null) => SqlBuilder.PostgreSql(options);

    public static TheoryData<SqlTestCase> DeleteWithExplicitAliasData =>
    [
        new SqlTestCase(
            expectedSql: [
                """
                DELETE FROM "dbo"."Orders" AS "o"
                WHERE "o"."Id" = $1
                """
            ],
            expectedParameters: _expectedParameters
        )
    ];

    public static TheoryData<SqlTestCase> DeleteWithoutAliasData =>
    [
        new SqlTestCase(
            expectedSql: [
                """
                DELETE FROM "dbo"."Orders"
                WHERE "dbo"."Orders"."Id" = $1
                """
            ],
            expectedParameters: _expectedParameters
        )
    ];
}