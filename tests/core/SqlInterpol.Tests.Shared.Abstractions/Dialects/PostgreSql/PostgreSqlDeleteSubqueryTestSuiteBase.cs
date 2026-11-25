using SqlInterpol.Configuration;
using SqlInterpol.Testing.Specifications;
using SqlInterpol.Testing.Xunit;
using Xunit;

namespace SqlInterpol.Tests.Dialects.PostgreSql;

public abstract partial class PostgreSqlDeleteSubqueryTestSuiteBase : IDeleteSubqueryTestSuite
{
    private static readonly object[] _expectedParameters = ["Cancelled"];

    public virtual SqlBuilder CreateBuilder(SqlInterpolOptions? options = null) => SqlBuilder.PostgreSql(options);

    public static TheoryData<SqlTestCase> Delete_WithSubqueryData =>
    [
        new SqlTestCase(
            expectedSql: [
                """
                DELETE FROM "OrderLine"
                WHERE "OrderLine"."OrderId" IN (
                    SELECT "dbo"."Orders"."Id"
                    FROM "dbo"."Orders"
                    WHERE "dbo"."Orders"."order_status" = $1
                )
                """
            ],
            expectedParameters: _expectedParameters
        )
    ];
}