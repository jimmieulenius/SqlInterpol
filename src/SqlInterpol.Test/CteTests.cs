using SqlInterpol.Test.Dialects;
using SqlInterpol.Test.Models;
using Xunit;

namespace SqlInterpol.Test;

public class CteTests
{
    private const int Threshold = 100;

    [Theory]
    [MemberData(nameof(Select_WithCteData))]
    public void Select_WithCte(SqlTestCase testCase)
    {
        // Arrange
        var db = testCase.CreateBuilder();
        
        // Define the inner query for the CTE
        var innerQuery = db
            .Entity<Product>(out var p)
            .Subquery(p, sub => sub.Append($$"""
                SELECT {{p.CategoryId}}, SUM({{p.Price}}) AS TotalPrice
                FROM {{p}} AS {{"p"}}
                GROUP BY {{p.CategoryId}}
                """));

        // Act
        testCase.Action(() => 
        {
            db.Entity<Category>(out var c);
            db.Entity<CategoryStats>(out var cs);

            // The parser sniffs "WITH" and treats cs as a CTE role
            return db.Append($$"""
                WITH {{cs}} AS (
                    {{innerQuery}}
                )
                SELECT {{c.Name}}, {{cs.TotalPrice}}
                FROM {{c}} AS {{"c"}}
                JOIN {{cs}} AS {{"cs"}}
                    ON {{c.Id}} = {{cs.CategoryId}}
                """).Build();
        });

        // Assert
        testCase.Assert();
    }

    [Theory]
    [MemberData(nameof(Select_WithCteData))]
    public void Select_WithCte_AutoAliased(SqlTestCase testCase)
    {
        // Arrange
        var db = testCase.CreateBuilder();
        db.Context.Options.EntityAutoAliasing = true;
        
        // Define the inner query for the CTE
        var innerQuery = db
            .Entity<Product>(out var p)
            .Subquery(p, sub => sub.Append($$"""
                SELECT {{p.CategoryId}}, SUM({{p.Price}}) AS TotalPrice
                FROM {{p}}
                GROUP BY {{p.CategoryId}}
                """));

        // Act
        testCase.Action(() => 
        {
            db.Entity<Category>(out var c);
            db.Entity<CategoryStats>(out var cs);

            // Auto-aliasing detects the FROM and JOIN locations and injects "AS c" and "AS cs" automatically!
            return db.Append($$"""
                WITH {{cs}} AS (
                    {{innerQuery}}
                )
                SELECT {{c.Name}}, {{cs.TotalPrice}}
                FROM {{c}}
                JOIN {{cs}}
                    ON {{c.Id}} = {{cs.CategoryId}}
                """).Build();
        });

        // Assert
        testCase.Assert();
    }

    [Theory]
    [MemberData(nameof(Select_WithRecursiveCteData))]
    public void Select_WithRecursiveCte(SqlTestCase testCase)
    {
        // Arrange
        var db = testCase.CreateBuilder();

        // Act
        testCase.Action(() => db.Append($"""
            WITH RECURSIVE Numbers AS (
                SELECT 1 AS n
                UNION ALL
                SELECT n + 1 FROM Numbers WHERE n < 10
            )
            SELECT n FROM Numbers
            """).Build()
        );

        // Assert
        testCase.Assert();
    }

    [Theory]
    [MemberData(nameof(RawCteData))]
    public void Select_WithRawCte(SqlTestCase testCase)
    {
        // Arrange
        var db = testCase.CreateBuilder();

        // Act
        testCase.Action(() => 
        {
            db.Entity<Product>(out var p);

            return db.Append($$"""
                WITH ExpensiveProducts AS (
                    SELECT * FROM {{p}} WHERE {{p.Price}} > {{Threshold}}
                )
                SELECT * FROM ExpensiveProducts
                """).Build();
        });

        // Assert
        testCase.Assert();
    }

    public static TheoryData<SqlTestCase> Select_WithCteData
    {
        get
        {
            return
            [
                new SqlTestCase(
                    SqlDialectKind.CustomDb,
                    [
                        """
                        WITH <<CategoryStats>> AS (
                            SELECT <<p>>.<<CategoryId>>, SUM(<<p>>.<<Price>>) AS TotalPrice
                            FROM <<dbo>>.<<Products>> AS <<p>>
                            GROUP BY <<p>>.<<CategoryId>>
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
                            SELECT "p"."CategoryId", SUM("p"."Price") AS TotalPrice
                            FROM "dbo"."Products" AS "p"
                            GROUP BY "p"."CategoryId"
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
                            SELECT `p`.`CategoryId`, SUM(`p`.`Price`) AS TotalPrice
                            FROM `dbo`.`Products` AS `p`
                            GROUP BY `p`.`CategoryId`
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
                            SELECT "p"."CategoryId", SUM("p"."Price") AS TotalPrice
                            FROM "dbo"."Products" AS "p"
                            GROUP BY "p"."CategoryId"
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
                            SELECT "p"."CategoryId", SUM("p"."Price") AS TotalPrice
                            FROM "dbo"."Products" AS "p"
                            GROUP BY "p"."CategoryId"
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
                            SELECT "p"."CategoryId", SUM("p"."Price") AS TotalPrice
                            FROM "dbo"."Products" AS "p"
                            GROUP BY "p"."CategoryId"
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
                            SELECT [p].[CategoryId], SUM([p].[Price]) AS TotalPrice
                            FROM [dbo].[Products] AS [p]
                            GROUP BY [p].[CategoryId]
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
    }

    public static TheoryData<SqlTestCase> Select_WithRecursiveCteData
    {
        get
        {
            return
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
        }
    }

    public static TheoryData<SqlTestCase> RawCteData
    {
        get
        {
            object?[] expectedParams = [Threshold];

            return
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
                    ],
                    expectedParameters: expectedParams
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
                    ],
                    expectedParameters: expectedParams
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
                    ],
                    expectedParameters: expectedParams
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
                    ],
                    expectedParameters: expectedParams
                ),
                new SqlTestCase(
                    SqlDialectKind.SqLite,
                    [
                        """
                        WITH ExpensiveProducts AS (
                            SELECT * FROM "dbo"."Products" WHERE "dbo"."Products"."Price" > @p1
                        )
                        SELECT * FROM ExpensiveProducts
                        """
                    ],
                    expectedParameters: expectedParams
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
                    ],
                    expectedParameters: expectedParams
                )
            ];
        }
    }
}