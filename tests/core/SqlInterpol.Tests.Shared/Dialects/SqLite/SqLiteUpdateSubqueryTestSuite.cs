using SqlInterpol.Configuration;
using SqlInterpol.Testing.Specifications;
using SqlInterpol.Testing.Xunit;
using Xunit;

namespace SqlInterpol.Tests.Dialects.SqLite;

public partial class SqLiteUpdateSubqueryTestSuite : IUpdateSubqueryTestSuite
{
    public SqlBuilder CreateBuilder(SqlInterpolOptions? options = null) => SqlBuilder.SqLite(options);

    public static TheoryData<SqlTestCase> UpdateSubqueryData => [new SqlTestCase(
        expectedSql: [
            """
            WITH "stats" AS (
                SELECT CategoryId, MAX(Price) AS max_price FROM Products GROUP BY CategoryId
            )
            UPDATE "stats"
            SET "max_price" = @p1
            WHERE "stats"."CategoryId" = 5
            """
        ],
        expectedParameters: [99.99m]
    )];

    public static TheoryData<SqlTestCase> UpdateTypeSafeSubqueryData => [new SqlTestCase(
        expectedSql: [
            """
            WITH "stats" AS (
                SELECT
                    "p"."CategoryId" AS "CategoryId",
                    MAX("p"."Price") AS "MaxPrice"
                FROM "dbo"."Products" AS "p"
                GROUP BY "p"."CategoryId"
            )
            UPDATE "stats"
            SET "max_price" = @p1
            WHERE "stats"."CategoryId" = 5
            """
        ],
        expectedParameters: [99.99m]
    )];
}