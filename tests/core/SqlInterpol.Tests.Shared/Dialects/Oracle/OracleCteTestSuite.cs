using SqlInterpol.Configuration;
using SqlInterpol.Testing.Specifications;
using SqlInterpol.Testing.Xunit;
using Xunit;

namespace SqlInterpol.Tests.Dialects.Oracle;

public partial class OracleCteTestSuite : ICteTestSuite
{
    private static readonly object[] _expectedParameters = [100];

    public SqlBuilder CreateBuilder(SqlInterpolOptions? options = null) => SqlBuilder.Oracle(options);

    public static TheoryData<SqlTestCase> Select_WithCteData =>
    [
        new SqlTestCase(
            expectedSql: [
                """
                WITH "CategoryStats" AS (
                    SELECT "p"."CategoryId", SUM("p"."Price") AS "TotalPrice"
                    FROM "dbo"."Products" "p"
                    GROUP BY "p"."CategoryId"
                )
                SELECT "c"."Name", "cs"."TotalPrice"
                FROM "Category" "c"
                JOIN "CategoryStats" "cs"
                    ON "c"."Id" = "cs"."CategoryId"
                """
            ]
        )
    ];

    public static TheoryData<SqlTestCase> Select_WithRecursiveCteData =>
    [
        new SqlTestCase(
            expectedSql: [
                """
                WITH Numbers AS (
                    SELECT 1 AS n
                    UNION ALL
                    SELECT n + 1 FROM Numbers WHERE n < 10
                )
                SELECT n FROM Numbers
                """
            ]
        )
    ];

    public static TheoryData<SqlTestCase> RawCteData =>
    [
        new SqlTestCase(
            expectedSql: [
                """
                WITH ExpensiveProducts AS (
                    SELECT * FROM "dbo"."Products" WHERE "dbo"."Products"."Price" > :0
                )
                SELECT * FROM ExpensiveProducts
                """
            ],
            expectedParameters: _expectedParameters
        )
    ];
}