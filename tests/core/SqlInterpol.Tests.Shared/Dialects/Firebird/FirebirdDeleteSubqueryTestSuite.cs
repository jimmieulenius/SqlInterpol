using SqlInterpol.Configuration;
using SqlInterpol.Testing.Specifications;
using SqlInterpol.Testing.Xunit;
using Xunit;

namespace SqlInterpol.Tests.Dialects.Firebird;

public partial class FirebirdDeleteSubqueryTestSuite : IDeleteSubqueryTestSuite
{
    private static readonly object[] _expectedParameters = ["Cancelled"];

    public SqlBuilder CreateBuilder(SqlInterpolOptions? options = null) => SqlBuilder.Firebird(options);

    public static TheoryData<SqlTestCase> Delete_WithSubqueryData =>
    [
        new SqlTestCase(
            expectedSql: [
                """
                DELETE FROM "OrderLine"
                WHERE "OrderLine"."OrderId" IN (
                    SELECT "dbo"."Orders"."Id"
                    FROM "dbo"."Orders"
                    WHERE "dbo"."Orders"."order_status" = @p0
                )
                """
            ],
            expectedParameters: _expectedParameters
        )
    ];
}