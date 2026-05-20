using SqlInterpol.Test.Dialects;
using SqlInterpol.Test.Models;

namespace SqlInterpol.Test;

public class JoinSubqueryTests
{
    [Theory]
    [MemberData(nameof(JoinSubqueryData))]
    public void Join_Subquery_WithTypeSafeProjection(SqlTestCase testCase)
    {
        // Arrange
        var db = testCase.CreateBuilder();

        var statsQuery = db
            .Entity<CategoryStats>(alias: "stats")
            .Entity<Product>(alias: "p")
            .Query((stats, p) => db.Append($$"""
                SELECT 
                    {{p[x => x.CategoryId]}} AS {{stats[x => x.CategoryId]}},
                    SUM({{p[x => x.Price]}}) AS {{stats[x => x.TotalPrice]}}
                FROM {{p}}
                GROUP BY {{p[x => x.CategoryId]}}
                """));

        // Act
        var result = db.Entity<Category>(alias: "c").Query(c => db.Append($$"""
            SELECT 
                {{c[x => x.Name]}}, 
                {{statsQuery[x => x.TotalPrice]}}
            FROM {{c}}
            LEFT JOIN
            (
                {{statsQuery}}
            )
                ON {{statsQuery[x => x.CategoryId]}} = {{c[x => x.Id]}}
            """))
            .Build();

        // Assert
        testCase.AssertSql(result.Sql);
    }

    public static TheoryData<SqlTestCase> JoinSubqueryData =>
    [
        new SqlTestCase(
            SqlDialectKind.CustomDb,
            [
                """
                SELECT 
                    <<c>>.<<Name>>, 
                    <<stats>>.<<TotalPrice>>
                FROM <<Category>> AS <<c>>
                LEFT JOIN
                (
                    SELECT 
                        <<p>>.<<CategoryId>> AS <<CategoryId>>,
                        SUM(<<p>>.<<Price>>) AS <<TotalPrice>>
                    FROM <<dbo>>.<<Products>> AS <<p>>
                    GROUP BY <<p>>.<<CategoryId>>
                ) AS <<stats>>
                    ON <<stats>>.<<CategoryId>> = <<c>>.<<Id>>
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.Firebird,
            [
                """
                SELECT 
                    "c"."Name", 
                    "stats"."TotalPrice"
                FROM "Category" AS "c"
                LEFT JOIN
                (
                    SELECT 
                        "p"."CategoryId" AS "CategoryId",
                        SUM("p"."Price") AS "TotalPrice"
                    FROM "dbo"."Products" AS "p"
                    GROUP BY "p"."CategoryId"
                ) AS "stats"
                    ON "stats"."CategoryId" = "c"."Id"
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.MySql,
            [
                """
                SELECT 
                    `c`.`Name`, 
                    `stats`.`TotalPrice`
                FROM `Category` AS `c`
                LEFT JOIN
                (
                    SELECT 
                        `p`.`CategoryId` AS `CategoryId`,
                        SUM(`p`.`Price`) AS `TotalPrice`
                    FROM `dbo`.`Products` AS `p`
                    GROUP BY `p`.`CategoryId`
                ) AS `stats`
                    ON `stats`.`CategoryId` = `c`.`Id`
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.Oracle,
            [
                """
                SELECT 
                    "c"."Name", 
                    "stats"."TotalPrice"
                FROM "Category" AS "c"
                LEFT JOIN
                (
                    SELECT 
                        "p"."CategoryId" AS "CategoryId",
                        SUM("p"."Price") AS "TotalPrice"
                    FROM "dbo"."Products" AS "p"
                    GROUP BY "p"."CategoryId"
                ) AS "stats"
                    ON "stats"."CategoryId" = "c"."Id"
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.PostgreSql,
            [
                """
                SELECT 
                    "c"."Name", 
                    "stats"."TotalPrice"
                FROM "Category" AS "c"
                LEFT JOIN
                (
                    SELECT 
                        "p"."CategoryId" AS "CategoryId",
                        SUM("p"."Price") AS "TotalPrice"
                    FROM "dbo"."Products" AS "p"
                    GROUP BY "p"."CategoryId"
                ) AS "stats"
                    ON "stats"."CategoryId" = "c"."Id"
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.SqLite,
            [
                """
                SELECT 
                    "c"."Name", 
                    "stats"."TotalPrice"
                FROM "Category" AS "c"
                LEFT JOIN
                (
                    SELECT 
                        "p"."CategoryId" AS "CategoryId",
                        SUM("p"."Price") AS "TotalPrice"
                    FROM "dbo"."Products" AS "p"
                    GROUP BY "p"."CategoryId"
                ) AS "stats"
                    ON "stats"."CategoryId" = "c"."Id"
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.SqlServer,
            [
                """
                SELECT 
                    [c].[Name], 
                    [stats].[TotalPrice]
                FROM [Category] AS [c]
                LEFT JOIN
                (
                    SELECT 
                        [p].[CategoryId] AS [CategoryId],
                        SUM([p].[Price]) AS [TotalPrice]
                    FROM [dbo].[Products] AS [p]
                    GROUP BY [p].[CategoryId]
                ) AS [stats]
                    ON [stats].[CategoryId] = [c].[Id]
                """
            ]
        )
    ];
}