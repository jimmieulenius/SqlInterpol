using SqlInterpol.Config;
using SqlInterpol.Metadata;
using SqlInterpol.Test.Dialects;
using SqlInterpol.Test.Models;

namespace SqlInterpol.Test;

public class CteTests
{
    [SqlTable("CategoryStats", Schema = "dbo")]
    public record CategoryStats
    {
        public int CategoryId { get; init; }
        public decimal TotalPrice { get; init; }
    }

    [Theory]
    [MemberData(nameof(AllDialectsCteData))]
    public void Select_WithCte_AllDialects_RendersCorrectly(SqlTestCase testCase)
    {
        // Arrange
        // Create the builder for the specific dialect provided by the test case
        var db = testCase.CreateBuilder();
        
        // 1. Define the inner query for the CTE
        var innerQuery = db.Query<Product>(p => db.Append($$"""
            SELECT {{p[x => x.CategoryId]}}, SUM({{p[x => x.Price]}}) AS TotalPrice
            FROM {{p}}
            GROUP BY {{p[x => x.CategoryId]}}
            """));

        // Act
        // 2. Setup the main query. The parser will sniff "WITH" and treat cte as a CTE role
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
        // Normalize newlines for cross-platform comparison
        var expectedSql = testCase.ExpectedSql[0].Replace("\r\n", "\n");
        var actualSql = result.Sql.Replace("\r\n", "\n");

        Assert.Equal(expectedSql, actualSql);
    }

    public static TheoryData<SqlTestCase> AllDialectsCteData =>
    [
        // SQL Server using [brackets]
        new SqlTestCase(SqlDialectKind.SqlServer, [
            """
            WITH [CategoryStats] AS (
                SELECT [dbo].[Products].[CategoryId], SUM([dbo].[Products].[Price]) AS TotalPrice
                FROM [dbo].[Products]
                GROUP BY [dbo].[Products].[CategoryId]
            )
            SELECT [c].[Name], [cs].[TotalPrice]
            FROM [dbo].[Categories] AS [c]
            JOIN [CategoryStats] AS [cs]
                ON [c].[Id] = [cs].[CategoryId]
            """
        ]),

        // Postgres using "double quotes"
        new SqlTestCase(SqlDialectKind.PostgreSql, [
            """
            WITH "CategoryStats" AS (
                SELECT "dbo"."Products"."CategoryId", SUM("dbo"."Products"."Price") AS TotalPrice
                FROM "dbo"."Products"
                GROUP BY "dbo"."Products"."CategoryId"
            )
            SELECT "c"."Name", "cs"."TotalPrice"
            FROM "dbo"."Categories" AS "c"
            JOIN "CategoryStats" AS "cs"
                ON "c"."Id" = "cs"."CategoryId"
            """
        ]),

        // SQLite using "double quotes" (standard)
        new SqlTestCase(SqlDialectKind.SqLite, [
            """
            WITH "CategoryStats" AS (
                SELECT "dbo"."Products"."CategoryId", SUM("dbo"."Products"."Price") AS TotalPrice
                FROM "dbo"."Products"
                GROUP BY "dbo"."Products"."CategoryId"
            )
            SELECT "c"."Name", "cs"."TotalPrice"
            FROM "dbo"."Categories" AS "c"
            JOIN "CategoryStats" AS "cs"
                ON "c"."Id" = "cs"."CategoryId"
            """
        ]),

        // MySQL using `backticks`
        new SqlTestCase(SqlDialectKind.MySql, [
            """
            WITH `CategoryStats` AS (
                SELECT `dbo`.`Products`.`CategoryId`, SUM(`dbo`.`Products`.`Price`) AS TotalPrice
                FROM `dbo`.`Products`
                GROUP BY `dbo`.`Products`.`CategoryId`
            )
            SELECT `c`.`Name`, `cs`.`TotalPrice`
            FROM `dbo`.`Categories` AS `c`
            JOIN `CategoryStats` AS `cs`
                ON `c`.`Id` = `cs`.`CategoryId`
            """
        ]),

        // Oracle using "double quotes" (standard)
        new SqlTestCase(SqlDialectKind.Oracle, [
            """
            WITH "CategoryStats" AS (
                SELECT "dbo"."Products"."CategoryId", SUM("dbo"."Products"."Price") AS TotalPrice
                FROM "dbo"."Products"
                GROUP BY "dbo"."Products"."CategoryId"
            )
            SELECT "c"."Name", "cs"."TotalPrice"
            FROM "dbo"."Categories" AS "c"
            JOIN "CategoryStats" AS "cs"
                ON "c"."Id" = "cs"."CategoryId"
            """
        ]),

        // Custom Dialect using <<custom>> brackets
        new SqlTestCase(SqlDialectKind.CustomDb, [
            """
            WITH <<CategoryStats>> AS (
                SELECT <<dbo>>.<<Products>>.<<CategoryId>>, SUM(<<dbo>>.<<Products>>.<<Price>>) AS TotalPrice
                FROM <<dbo>>.<<Products>>
                GROUP BY <<dbo>>.<<Products>>.<<CategoryId>>
            )
            SELECT <<c>>.<<Name>>, <<cs>>.<<TotalPrice>>
            FROM <<dbo>>.<<Categories>> AS <<c>>
            JOIN <<CategoryStats>> AS <<cs>>
                ON <<c>>.<<Id>> = <<cs>>.<<CategoryId>>
            """
        ])
    ];
}