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
            SqlDialectKind.Firebird,
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

    [Theory]
    [MemberData(nameof(Select_WithRecursiveCteData))]
    public void Select_WithRecursiveCte(SqlTestCase testCase)
    {
        // Arrange
        var db = testCase.CreateBuilder();

        // Act
        db.Append($"""
            WITH RECURSIVE Numbers AS (
                SELECT 1 AS n
                UNION ALL
                SELECT n + 1 FROM Numbers WHERE n < 10
            )
            SELECT n FROM Numbers
            """);
        var result = db.Build();

        // Assert
        testCase.AssertSql(result.Sql);
    }

    public static TheoryData<SqlTestCase> Select_WithRecursiveCteData =>
    [
        new SqlTestCase(
            SqlDialectKind.CustomDb,
            [
                """
                WITH RECURSIVE Numbers AS (
                    SELECT 1 AS n
                    UNION ALL
                    SELECT n + 1 FROM Numbers WHERE n < 10
                )
                SELECT n FROM Numbers
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.Firebird,
            [
                """
                WITH RECURSIVE Numbers AS (
                    SELECT 1 AS n
                    UNION ALL
                    SELECT n + 1 FROM Numbers WHERE n < 10
                )
                SELECT n FROM Numbers
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.MySql,
            [
                """
                WITH RECURSIVE Numbers AS (
                    SELECT 1 AS n
                    UNION ALL
                    SELECT n + 1 FROM Numbers WHERE n < 10
                )
                SELECT n FROM Numbers
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.Oracle,
            [
                """
                WITH Numbers AS (
                    SELECT 1 AS n
                    UNION ALL
                    SELECT n + 1 FROM Numbers WHERE n < 10
                )
                SELECT n FROM Numbers
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.PostgreSql,
            [
                """
                WITH RECURSIVE Numbers AS (
                    SELECT 1 AS n
                    UNION ALL
                    SELECT n + 1 FROM Numbers WHERE n < 10
                )
                SELECT n FROM Numbers
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.SqLite,
            [
                """
                WITH RECURSIVE Numbers AS (
                    SELECT 1 AS n
                    UNION ALL
                    SELECT n + 1 FROM Numbers WHERE n < 10
                )
                SELECT n FROM Numbers
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.SqlServer,
            [
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

    [Theory]
    [MemberData(nameof(RawCteData))]
    public void Select_WithRawCte(SqlTestCase testCase)
    {
        // Arrange
        var db = testCase.CreateBuilder();
        var threshold = 100;

        // Act
        var result = db.Query<Product>(p => db.Append($$"""
            WITH ExpensiveProducts AS (
                SELECT * FROM {{p}} WHERE {{p[x => x.Price]}} > {{threshold}}
            )
            SELECT * FROM ExpensiveProducts
            """)).Build();

        // Assert
        testCase.AssertSql(result.Sql);
        Assert.Single(result.Parameters);
    }

    public static TheoryData<SqlTestCase> RawCteData =>
    [
        new SqlTestCase(
            SqlDialectKind.Firebird,
            [
                """
                WITH ExpensiveProducts AS (
                    SELECT * FROM "dbo"."Products" WHERE "dbo"."Products"."Price" > @p0
                )
                SELECT * FROM ExpensiveProducts
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.MySql,
            [
                """
                WITH ExpensiveProducts AS (
                    SELECT * FROM `dbo`.`Products` WHERE `dbo`.`Products`.`Price` > @p0
                )
                SELECT * FROM ExpensiveProducts
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.Oracle,
            [
                """
                WITH ExpensiveProducts AS (
                    SELECT * FROM "dbo"."Products" WHERE "dbo"."Products"."Price" > :0
                )
                SELECT * FROM ExpensiveProducts
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.PostgreSql,
            [
                """
                WITH ExpensiveProducts AS (
                    SELECT * FROM "dbo"."Products" WHERE "dbo"."Products"."Price" > $1
                )
                SELECT * FROM ExpensiveProducts
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.SqLite,
            [
                """
                WITH ExpensiveProducts AS (
                    SELECT * FROM "dbo"."Products" WHERE "dbo"."Products"."Price" > ?0
                )
                SELECT * FROM ExpensiveProducts
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.SqlServer,
            [
                """
                WITH ExpensiveProducts AS (
                    SELECT * FROM [dbo].[Products] WHERE [dbo].[Products].[Price] > @p0
                )
                SELECT * FROM ExpensiveProducts
                """
            ]
        )
    ];
}