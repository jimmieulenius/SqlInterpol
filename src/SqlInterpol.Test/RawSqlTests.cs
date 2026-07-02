using SqlInterpol.Test.Models;

namespace SqlInterpol.Test;

public class RawSqlTests
{
    [Theory]
    [MemberData(nameof(ComplexRawSqlData))]
    public void RawSql_ComplexStatements(SqlTestCase testCase)
    {
        // Arrange
        var db = testCase.CreateBuilder();
        var minPrice = 50.00m;

        // Act
        // We are mixing AST nodes {{p}}, parameters {{minPrice}}, and RAW SQL here!
        testCase.Action(() => db.Entity<Product>(out var p)
            .Append($$"""
            SELECT {{p.Id}}, {{p.Name}}
            FROM {{p}}
            WHERE {{p.Price}} > {{minPrice}}
              {{Sql.Raw("AND p.Status = 'ACTIVE'")}} /* Raw SQL condition */
            GROUP BY {{p.Id}}, {{p.Name}}
            HAVING COUNT(*) > 1
            ORDER BY {{p.Name}} DESC
            LIMIT 10 OFFSET 5
            """)
            .Build()
        );

        // Assert - Natively verifies the SQL string AND the expected parameters array!
        testCase.Assert();
    }

    [Theory]
    [MemberData(nameof(WindowFunctionData))]
    public void RawSql_WindowFunctions(SqlTestCase testCase)
    {
        // Arrange
        var db = testCase.CreateBuilder();

        // Act
        // Proving that window functions and raw SQL keywords flow perfectly around our AST tags
        testCase.Action(() => db.Entity<Product>(out var p)
            .Append($$"""
            SELECT 
                {{p.Name}},
                {{p.Price}},
                AVG({{p.Price}}) OVER (PARTITION BY {{p.CategoryId}}) as AvgCategoryPrice
            FROM {{p}}
            """)
            .Build()
        );

        // Assert
        testCase.Assert();
    }

    // --- TEST DATA ---

    public static TheoryData<SqlTestCase> ComplexRawSqlData
    {
        get
        {
            object?[] expectedParams = [50.00m];

            return
            [
                new SqlTestCase(
                    SqlDialectKind.Firebird,
                    [
                        """
                        SELECT "dbo"."Products"."Id", "dbo"."Products"."PROD_NAME"
                        FROM "dbo"."Products"
                        WHERE "dbo"."Products"."Price" > @p0
                          AND p.Status = 'ACTIVE' /* Raw SQL condition */
                        GROUP BY "dbo"."Products"."Id", "dbo"."Products"."PROD_NAME"
                        HAVING COUNT(*) > 1
                        ORDER BY "dbo"."Products"."PROD_NAME" DESC
                        LIMIT 10 OFFSET 5
                        """
                    ],
                    expectedParameters: expectedParams
                ),
                new SqlTestCase(
                    SqlDialectKind.MySql,
                    [
                        """
                        SELECT `dbo`.`Products`.`Id`, `dbo`.`Products`.`PROD_NAME`
                        FROM `dbo`.`Products`
                        WHERE `dbo`.`Products`.`Price` > @p0
                          AND p.Status = 'ACTIVE' /* Raw SQL condition */
                        GROUP BY `dbo`.`Products`.`Id`, `dbo`.`Products`.`PROD_NAME`
                        HAVING COUNT(*) > 1
                        ORDER BY `dbo`.`Products`.`PROD_NAME` DESC
                        LIMIT 10 OFFSET 5
                        """
                    ],
                    expectedParameters: expectedParams
                ),
                new SqlTestCase(
                    SqlDialectKind.Oracle,
                    [
                        """
                        SELECT "dbo"."Products"."Id", "dbo"."Products"."PROD_NAME"
                        FROM "dbo"."Products"
                        WHERE "dbo"."Products"."Price" > :0
                          AND p.Status = 'ACTIVE' /* Raw SQL condition */
                        GROUP BY "dbo"."Products"."Id", "dbo"."Products"."PROD_NAME"
                        HAVING COUNT(*) > 1
                        ORDER BY "dbo"."Products"."PROD_NAME" DESC
                        LIMIT 10 OFFSET 5
                        """
                    ],
                    expectedParameters: expectedParams
                ),
                new SqlTestCase(
                    SqlDialectKind.PostgreSql,
                    [
                        """
                        SELECT "dbo"."Products"."Id", "dbo"."Products"."PROD_NAME"
                        FROM "dbo"."Products"
                        WHERE "dbo"."Products"."Price" > $1
                          AND p.Status = 'ACTIVE' /* Raw SQL condition */
                        GROUP BY "dbo"."Products"."Id", "dbo"."Products"."PROD_NAME"
                        HAVING COUNT(*) > 1
                        ORDER BY "dbo"."Products"."PROD_NAME" DESC
                        LIMIT 10 OFFSET 5
                        """
                    ],
                    expectedParameters: expectedParams
                ),
                new SqlTestCase(
                    SqlDialectKind.SqLite,
                    [
                        """
                        SELECT "dbo"."Products"."Id", "dbo"."Products"."PROD_NAME"
                        FROM "dbo"."Products"
                        WHERE "dbo"."Products"."Price" > @p1
                          AND p.Status = 'ACTIVE' /* Raw SQL condition */
                        GROUP BY "dbo"."Products"."Id", "dbo"."Products"."PROD_NAME"
                        HAVING COUNT(*) > 1
                        ORDER BY "dbo"."Products"."PROD_NAME" DESC
                        LIMIT 10 OFFSET 5
                        """
                    ],
                    expectedParameters: expectedParams
                ),
                new SqlTestCase(
                    SqlDialectKind.SqlServer,
                    [
                        """
                        SELECT [dbo].[Products].[Id], [dbo].[Products].[PROD_NAME]
                        FROM [dbo].[Products]
                        WHERE [dbo].[Products].[Price] > @p0
                          AND p.Status = 'ACTIVE' /* Raw SQL condition */
                        GROUP BY [dbo].[Products].[Id], [dbo].[Products].[PROD_NAME]
                        HAVING COUNT(*) > 1
                        ORDER BY [dbo].[Products].[PROD_NAME] DESC
                        LIMIT 10 OFFSET 5
                        """
                    ],
                    expectedParameters: expectedParams
                )
            ];
        }
    }

    public static TheoryData<SqlTestCase> WindowFunctionData =>
    [
        new SqlTestCase(
            SqlDialectKind.Firebird,
            [
                """
                SELECT 
                    "dbo"."Products"."PROD_NAME",
                    "dbo"."Products"."Price",
                    AVG("dbo"."Products"."Price") OVER (PARTITION BY "dbo"."Products"."CategoryId") as AvgCategoryPrice
                FROM "dbo"."Products"
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.MySql,
            [
                """
                SELECT 
                    `dbo`.`Products`.`PROD_NAME`,
                    `dbo`.`Products`.`Price`,
                    AVG(`dbo`.`Products`.`Price`) OVER (PARTITION BY `dbo`.`Products`.`CategoryId`) as AvgCategoryPrice
                FROM `dbo`.`Products`
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.Oracle,
            [
                """
                SELECT 
                    "dbo"."Products"."PROD_NAME",
                    "dbo"."Products"."Price",
                    AVG("dbo"."Products"."Price") OVER (PARTITION BY "dbo"."Products"."CategoryId") as AvgCategoryPrice
                FROM "dbo"."Products"
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.PostgreSql,
            [
                """
                SELECT 
                    "dbo"."Products"."PROD_NAME",
                    "dbo"."Products"."Price",
                    AVG("dbo"."Products"."Price") OVER (PARTITION BY "dbo"."Products"."CategoryId") as AvgCategoryPrice
                FROM "dbo"."Products"
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.SqLite,
            [
                """
                SELECT 
                    "dbo"."Products"."PROD_NAME",
                    "dbo"."Products"."Price",
                    AVG("dbo"."Products"."Price") OVER (PARTITION BY "dbo"."Products"."CategoryId") as AvgCategoryPrice
                FROM "dbo"."Products"
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.SqlServer,
            [
                """
                SELECT 
                    [dbo].[Products].[PROD_NAME],
                    [dbo].[Products].[Price],
                    AVG([dbo].[Products].[Price]) OVER (PARTITION BY [dbo].[Products].[CategoryId]) as AvgCategoryPrice
                FROM [dbo].[Products]
                """
            ]
        )
    ];
}