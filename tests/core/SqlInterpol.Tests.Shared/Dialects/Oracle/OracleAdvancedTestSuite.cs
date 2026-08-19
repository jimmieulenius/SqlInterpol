using SqlInterpol.Configuration;
using SqlInterpol.Testing.Specifications;
using SqlInterpol.Testing.Xunit;
using Xunit;

namespace SqlInterpol.Tests.Dialects.Oracle;

public partial class OracleAdvancedTestSuite : IAdvancedTestSuite
{
    public SqlBuilder CreateBuilder(SqlInterpolOptions? options = null) => SqlBuilder.Oracle(options);

    public static TheoryData<SqlTestCase> DynamicQueryData =>
    [
        new SqlTestCase(
            [
                """
                SELECT "stats"."OrderId", "stats"."TotalAmount"
                FROM (
                    SELECT 
                        "o"."CustomerId",
                        "o"."Id" AS "OrderId", 
                        SUM("ol"."Price") AS "TotalAmount"
                    FROM "dbo"."Orders" "o"
                    JOIN "OrderLine" "ol" ON "o"."Id" = "ol"."OrderId"
                    GROUP BY "o"."CustomerId", "o"."Id"
                ) "stats"
                WHERE "stats"."CustomerId" = :0
                ORDER BY "stats"."TotalAmount" DESC
                OFFSET :2 ROWS FETCH NEXT :1 ROWS ONLY
                """
            ]
        )
    ];

    public static TheoryData<SqlTestCase> AdvancedDynamicQueryData =>
    [
        new SqlTestCase(
            [
                """
                SELECT "stats"."OrderId", "stats"."ProductName", "stats"."TotalAmount"
                FROM (
                    SELECT 
                        "o"."Id" AS "OrderId", 
                        "p"."PROD_NAME" AS "ProductName",
                        "ol_agg"."TotalAmount" AS "TotalAmount"
                    FROM "dbo"."Orders" "o"
                    
                    JOIN (
                        SELECT 
                            "ol"."OrderId" AS "OrderId",
                            "ol"."ProductId" AS "ProductId",
                            SUM("ol"."Price") AS "TotalAmount"
                        -- JOIN ol_agg ON ...
                        FROM "OrderLine" "ol"
                        GROUP BY "ol"."OrderId", "ol"."ProductId"
                    ) "ol_agg" ON "o"."Id" = "ol_agg"."OrderId"
                    
                    JOIN "dbo"."Products" "p" ON "ol_agg"."ProductId" = "p"."Id"
                    JOIN "Category" "cat" ON "p"."CategoryId" = "cat"."Id"
                ) "stats"
                WHERE "stats"."ProductName" = :0
                ORDER BY "stats"."TotalAmount" DESC
                OFFSET :2 ROWS FETCH NEXT :1 ROWS ONLY
                """
            ]
        )
    ];

    public static TheoryData<SqlTestCase> ComplexRawSqlData =>
    [
        new SqlTestCase(
            [
                """
                SELECT "dbo"."Products"."Id", "dbo"."Products"."PROD_NAME"
                FROM "dbo"."Products"
                WHERE "dbo"."Products"."Price" > :0
                  AND p.Status = 'ACTIVE' /* Raw SQL condition */
                GROUP BY "dbo"."Products"."Id", "dbo"."Products"."PROD_NAME"
                HAVING COUNT(*) > 1
                ORDER BY "dbo"."Products"."PROD_NAME" DESC
                OFFSET 5 ROWS FETCH NEXT 10 ROWS ONLY
                """
            ]
        )
    ];
}