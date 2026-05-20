using SqlInterpol.Config;
using SqlInterpol.Test.Dialects;
using SqlInterpol.Test.Models;

namespace SqlInterpol.Test;

public class WindowFunctionTests
{
    [Theory]
    [MemberData(nameof(WindowFunctionData))]
    public void Select_WindowFunction_Wysiwyg_WorksAcrossDialects(SqlTestCase testCase)
    {
        // Arrange
        var db = testCase.CreateBuilder();

        // Act
        var result = db.Query<Product>(p => db.Append($$"""
            SELECT
                {{p[x => x.Name]}},
                SUM({{p[x => x.Price]}}) OVER (
                    PARTITION BY {{p[x => x.CategoryId]}}
                    ORDER BY {{p[x => x.Id]}} DESC
                ) AS CategoryTotal
            FROM {{p}}
            """)).Build();

        // Assert
        testCase.AssertSql(result.Sql);
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

    [Theory]
    [MemberData(nameof(RawWindowFunctionData))]
    public void Select_RawWindowFunction_PassesThrough(SqlTestCase testCase)
    {
        // Arrange
        var db = testCase.CreateBuilder();

        // Act
        var result = db.Query<Product>(p => db.Append($$"""
            SELECT 
                {{p[x => x.Name]}},
                {{p[x => x.Price]}},
                AVG({{p[x => x.Price]}}) OVER (PARTITION BY {{p[x => x.CategoryId]}}) as AvgCategoryPrice
            FROM {{p}}
            """)).Build();

        // Assert
        testCase.AssertSql(result.Sql);
    }

    public static TheoryData<SqlTestCase> RawWindowFunctionData =>
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