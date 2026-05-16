using SqlInterpol.Config;
using SqlInterpol.Test.Dialects;
using SqlInterpol.Test.Models;
using Xunit;

namespace SqlInterpol.Test;

public class CteTests
{
    [Theory]
    [MemberData(nameof(Select_WithCteData))]
    public void Select_WithCte(SqlTestCase testCase)
    {
        // Arrange
        var db = testCase.CreateBuilder();
        
        // Define the inner query for the CTE
        var innerQuery = db.Query<Product>(p => db.Append($$"""
            SELECT {{p[x => x.CategoryId]}}, SUM({{p[x => x.Price]}}) AS TotalPrice
            FROM {{p}}
            GROUP BY {{p[x => x.CategoryId]}}
            """));

        // Act
        // The parser sniffs "WITH" and treats cte as a CTE role
        var result = db.Query<Category, CategoryStats>((c, cte) => 
            db.Append($$"""
            WITH {{cte}} AS (
                {{innerQuery}}
            )
            SELECT {{c[x => x.Name]}}, {{cte[x => x.TotalPrice]}}
            FROM {{c}} AS {{"c"}}
            JOIN {{cte}} AS {{"cs"}}
                ON {{c[x => x.Id]}} = {{cte[x => x.CategoryId]}}
            """))
            .Build();

        // Assert
        testCase.AssertSql(result.Sql);
    }

    public static TheoryData<SqlTestCase> Select_WithCteData =>
    [
        new SqlTestCase(
            SqlDialectKind.CustomDb,
            [
                """
                WITH <<CategoryStats>> AS (
                    SELECT <<dbo>>.<<Products>>.<<CategoryId>>, SUM(<<dbo>>.<<Products>>.<<Price>>) AS TotalPrice
                    FROM <<dbo>>.<<Products>>
                    GROUP BY <<dbo>>.<<Products>>.<<CategoryId>>
                )
                SELECT <<c>>.<<Name>>, <<cs>>.<<TotalPrice>>
                FROM <<Category>> AS <<c>>
                JOIN <<CategoryStats>> AS <<cs>>
                    ON <<c>>.<<Id>> = <<cs>>.<<CategoryId>>
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.MySql,
            [
                """
                WITH `CategoryStats` AS (
                    SELECT `dbo`.`Products`.`CategoryId`, SUM(`dbo`.`Products`.`Price`) AS TotalPrice
                    FROM `dbo`.`Products`
                    GROUP BY `dbo`.`Products`.`CategoryId`
                )
                SELECT `c`.`Name`, `cs`.`TotalPrice`
                FROM `Category` AS `c`
                JOIN `CategoryStats` AS `cs`
                    ON `c`.`Id` = `cs`.`CategoryId`
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.Oracle,
            [
                """
                WITH "CategoryStats" AS (
                    SELECT "dbo"."Products"."CategoryId", SUM("dbo"."Products"."Price") AS TotalPrice
                    FROM "dbo"."Products"
                    GROUP BY "dbo"."Products"."CategoryId"
                )
                SELECT "c"."Name", "cs"."TotalPrice"
                FROM "Category" AS "c"
                JOIN "CategoryStats" AS "cs"
                    ON "c"."Id" = "cs"."CategoryId"
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.PostgreSql,
            [
                """
                WITH "CategoryStats" AS (
                    SELECT "dbo"."Products"."CategoryId", SUM("dbo"."Products"."Price") AS TotalPrice
                    FROM "dbo"."Products"
                    GROUP BY "dbo"."Products"."CategoryId"
                )
                SELECT "c"."Name", "cs"."TotalPrice"
                FROM "Category" AS "c"
                JOIN "CategoryStats" AS "cs"
                    ON "c"."Id" = "cs"."CategoryId"
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.SqLite,
            [
                """
                WITH "CategoryStats" AS (
                    SELECT "dbo"."Products"."CategoryId", SUM("dbo"."Products"."Price") AS TotalPrice
                    FROM "dbo"."Products"
                    GROUP BY "dbo"."Products"."CategoryId"
                )
                SELECT "c"."Name", "cs"."TotalPrice"
                FROM "Category" AS "c"
                JOIN "CategoryStats" AS "cs"
                    ON "c"."Id" = "cs"."CategoryId"
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.SqlServer,
            [
                """
                WITH [CategoryStats] AS (
                    SELECT [dbo].[Products].[CategoryId], SUM([dbo].[Products].[Price]) AS TotalPrice
                    FROM [dbo].[Products]
                    GROUP BY [dbo].[Products].[CategoryId]
                )
                SELECT [c].[Name], [cs].[TotalPrice]
                FROM [Category] AS [c]
                JOIN [CategoryStats] AS [cs]
                    ON [c].[Id] = [cs].[CategoryId]
                """
            ]
        )
    ];
}