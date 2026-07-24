using SqlInterpol.Configuration;
using SqlInterpol.Test.Dialects;
using SqlInterpol.Test.Models;

namespace SqlInterpol.Test;

public class WindowFunctionTests
{
    [Theory]
    [MemberData(nameof(WindowFunctionData))]
    public void Select_WindowFunction_Wysiwyg(SqlTestCase testCase)
    {
        // Arrange
        var db = testCase.CreateBuilder();

        // Act
        testCase.Action(() => db.Entity<Product>(out var p)
            .Append($$"""
            SELECT
                {{p.Name}},
                SUM({{p.Price}}) OVER (
                    PARTITION BY {{p.CategoryId}}
                    ORDER BY {{p.Id}} DESC
                ) AS CategoryTotal
            FROM {{p}}
            """)
            .Build()
        );

        // Assert
        testCase.Assert();
    }

    [Theory]
    [MemberData(nameof(RawWindowFunctionData))]
    public void Select_RawWindowFunction(SqlTestCase testCase)
    {
        // Arrange
        var db = testCase.CreateBuilder();

        // Act
        testCase.Action(() => db.Entity<Product>(out var p)
            .Append($$"""
            SELECT 
                {{p.Name}},
                {{p.Price}},
                AVG({{p.Price}}) OVER (PARTITION BY {{p.CategoryId}}) AS AvgCategoryPrice
            FROM {{p}}
            """)
            .Build()
        );

        // Assert
        testCase.Assert();
    }

    public static TheoryData<SqlTestCase> WindowFunctionData =>
    [
        new SqlTestCase(
            SqlDialectKind.CustomDb,
            [
                """
                SELECT
                    <<dbo>>.<<Products>>.<<PROD_NAME>>,
                    SUM(<<dbo>>.<<Products>>.<<Price>>) OVER (
                        PARTITION BY <<dbo>>.<<Products>>.<<CategoryId>>
                        ORDER BY <<dbo>>.<<Products>>.<<Id>> DESC
                    ) AS CategoryTotal
                FROM <<dbo>>.<<Products>>
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.Firebird,
            [
                """
                SELECT
                    "dbo"."Products"."PROD_NAME",
                    SUM("dbo"."Products"."Price") OVER (
                        PARTITION BY "dbo"."Products"."CategoryId"
                        ORDER BY "dbo"."Products"."Id" DESC
                    ) AS CategoryTotal
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
                    SUM(`dbo`.`Products`.`Price`) OVER (
                        PARTITION BY `dbo`.`Products`.`CategoryId`
                        ORDER BY `dbo`.`Products`.`Id` DESC
                    ) AS CategoryTotal
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
                    SUM("dbo"."Products"."Price") OVER (
                        PARTITION BY "dbo"."Products"."CategoryId"
                        ORDER BY "dbo"."Products"."Id" DESC
                    ) AS CategoryTotal
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
                    SUM("dbo"."Products"."Price") OVER (
                        PARTITION BY "dbo"."Products"."CategoryId"
                        ORDER BY "dbo"."Products"."Id" DESC
                    ) AS CategoryTotal
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
                    SUM("dbo"."Products"."Price") OVER (
                        PARTITION BY "dbo"."Products"."CategoryId"
                        ORDER BY "dbo"."Products"."Id" DESC
                    ) AS CategoryTotal
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
                    SUM([dbo].[Products].[Price]) OVER (
                        PARTITION BY [dbo].[Products].[CategoryId]
                        ORDER BY [dbo].[Products].[Id] DESC
                    ) AS CategoryTotal
                FROM [dbo].[Products]
                """
            ]
        )
    ];

    public static TheoryData<SqlTestCase> RawWindowFunctionData =>
    [
        new SqlTestCase(
            SqlDialectKind.CustomDb,
            [
                """
                SELECT 
                    <<dbo>>.<<Products>>.<<PROD_NAME>>,
                    <<dbo>>.<<Products>>.<<Price>>,
                    AVG(<<dbo>>.<<Products>>.<<Price>>) OVER (PARTITION BY <<dbo>>.<<Products>>.<<CategoryId>>) AS <<AvgCategoryPrice>>
                FROM <<dbo>>.<<Products>>
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.Firebird,
            [
                """
                SELECT 
                    "dbo"."Products"."PROD_NAME",
                    "dbo"."Products"."Price",
                    AVG("dbo"."Products"."Price") OVER (PARTITION BY "dbo"."Products"."CategoryId") AS "AvgCategoryPrice"
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
                    AVG(`dbo`.`Products`.`Price`) OVER (PARTITION BY `dbo`.`Products`.`CategoryId`) AS `AvgCategoryPrice`
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
                    AVG("dbo"."Products"."Price") OVER (PARTITION BY "dbo"."Products"."CategoryId") AS "AvgCategoryPrice"
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
                    AVG("dbo"."Products"."Price") OVER (PARTITION BY "dbo"."Products"."CategoryId") AS "AvgCategoryPrice"
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
                    AVG("dbo"."Products"."Price") OVER (PARTITION BY "dbo"."Products"."CategoryId") AS "AvgCategoryPrice"
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
                    AVG([dbo].[Products].[Price]) OVER (PARTITION BY [dbo].[Products].[CategoryId]) AS [AvgCategoryPrice]
                FROM [dbo].[Products]
                """
            ]
        )
    ];
}