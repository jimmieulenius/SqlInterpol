using SqlInterpol.Configuration;
using SqlInterpol.Testing.Specifications;
using SqlInterpol.Testing.Xunit;
using Xunit;

namespace SqlInterpol.Tests.Dialects.Oracle;

public partial class OracleUpdateSubqueryTestSuite : IUpdateSubqueryTestSuite
{
    public SqlBuilder CreateBuilder(SqlInterpolOptions? options = null) => SqlBuilder.Oracle(options);

    public static TheoryData<SqlTestCase> UpdateSubqueryData => [new SqlTestCase(
        expectedSql: [
            """
            UPDATE (SELECT CategoryId, MAX(Price) AS max_price FROM Products GROUP BY CategoryId) "stats"
            SET "max_price" = :0
            WHERE "stats"."CategoryId" = 5
            """
        ],
        expectedParameters: [99.99m]
    )];

    public static TheoryData<SqlTestCase> UpdateTypeSafeSubqueryData => [new SqlTestCase(
        expectedSql: [
            """
            UPDATE (SELECT "p"."CategoryId" AS "CategoryId", MAX("p"."Price") AS "MaxPrice" FROM "dbo"."Products" "p" GROUP BY "p"."CategoryId") "stats"
            SET "max_price" = :0
            WHERE "stats"."CategoryId" = 5
            """
        ],
        expectedParameters: [99.99m]
    )];
}