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

        // Act
        testCase.Action(() => db
            .Entity<CategoryStats>(out var stats)
            .Entity<Product>(out var p, "p")
            .Query(stats, out var statsQuery, () => db.Append($$"""
                SELECT 
                    {{p.CategoryId}} AS {{stats.CategoryId}},
                    SUM({{p.Price}}) AS {{stats.TotalPrice}}
                FROM {{p}}
                GROUP BY {{p.CategoryId}}
                """))
            .Entity<Category>(out var c, "c")
            .Append($$"""
                SELECT 
                    {{c.Name}}, 
                    {{stats.TotalPrice}}
                FROM {{c}}
                LEFT JOIN
                (
                    {{statsQuery}}
                ) AS stats
                    ON {{stats.CategoryId}} = {{c.Id}}
                """)
            .Build()
        );

        // Assert
        testCase.Assert();
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
                FROM "Category" "c"
                LEFT JOIN
                (
                    SELECT 
                        "p"."CategoryId" AS "CategoryId",
                        SUM("p"."Price") AS "TotalPrice"
                    FROM "dbo"."Products" "p"
                    GROUP BY "p"."CategoryId"
                ) "stats"
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