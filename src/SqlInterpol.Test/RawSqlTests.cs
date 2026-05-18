using System.Linq;
using SqlInterpol.Config;
using SqlInterpol.Test.Models;
using Xunit;

namespace SqlInterpol.Test;

public class RawSqlTests
{
    [Theory]
    [MemberData(nameof(ComplexRawSqlData))]
    public void RawSql_ComplexStatements_PassThroughUnmodified(SqlTestCase testCase)
    {
        // Arrange
        var db = testCase.CreateBuilder();
        var minPrice = 50.00m;

        // Act
        // We are mixing AST nodes {{p}}, parameters {{minPrice}}, and RAW SQL here!
        var result = db.Query<Product>(p => db.Append($$"""
            SELECT {{p[x => x.Id]}}, {{p[x => x.Name]}}
            FROM {{p}}
            WHERE {{p[x => x.Price]}} > {{minPrice}}
              AND p.Status = 'ACTIVE' /* Raw SQL condition */
            GROUP BY {{p[x => x.Id]}}, {{p[x => x.Name]}}
            HAVING COUNT(*) > 1
            ORDER BY {{p[x => x.Name]}} DESC
            LIMIT 10 OFFSET 5
            """)).Build();

        // Assert
        testCase.AssertSql(result.Sql);
        Assert.Single(result.Parameters);
        Assert.Equal(minPrice, result.Parameters.First().Value);
    }

    [Theory]
    [MemberData(nameof(TopKeywordData))]
    public void RawSql_TopKeyword_PassesThrough(SqlTestCase testCase)
    {
        // Arrange
        var db = testCase.CreateBuilder();
        
        // Act
        var result = db.Query<Product>(p => db.Append($$"""
            SELECT TOP 10 {{p[x => x.Id]}}
            FROM {{p}}
            """)).Build();

        // Assert
        testCase.AssertSql(result.Sql);
    }

    [Theory]
    [MemberData(nameof(OnDuplicateKeyData))]
    public void RawSql_OnDuplicateKey_PassesThrough(SqlTestCase testCase)
    {
        // Arrange
        var db = testCase.CreateBuilder();
        var id = 1;
        var newPrice = 99.99m;
        
        // Act
        var result = db.Query<Product>(p => db.Append($$"""
            INSERT INTO {{p}} (Id, Price) 
            VALUES ({{id}}, {{newPrice}}) 
            ON DUPLICATE KEY UPDATE Price = {{newPrice}}
            """)).Build();

        // Assert
        testCase.AssertSql(result.Sql);
        Assert.Equal(3, result.Parameters.Count);
    }

    [Theory]
    [MemberData(nameof(OnConflictData))]
    public void RawSql_OnConflictDoNothing_PassesThrough(SqlTestCase testCase)
    {
        // Arrange
        var db = testCase.CreateBuilder();
        var id = 1;
        
        // Act
        var result = db.Query<Product>(p => db.Append($$"""
            INSERT INTO {{p}} (Id) 
            VALUES ({{id}}) 
            ON CONFLICT DO NOTHING
            """)).Build();

        // Assert
        testCase.AssertSql(result.Sql);
        Assert.Single(result.Parameters);
    }

    [Theory]
    [MemberData(nameof(WindowFunctionData))]
    public void RawSql_WindowFunctions_PassThrough(SqlTestCase testCase)
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

    [Theory]
    [MemberData(nameof(CteData))]
    public void RawSql_CommonTableExpressions_PassThrough(SqlTestCase testCase)
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

    // --- TEST DATA ---

    public static TheoryData<SqlTestCase> ComplexRawSqlData =>
    [
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
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.SqLite,
            [
                """
                SELECT "dbo"."Products"."Id", "dbo"."Products"."PROD_NAME"
                FROM "dbo"."Products"
                WHERE "dbo"."Products"."Price" > ?0
                  AND p.Status = 'ACTIVE' /* Raw SQL condition */
                GROUP BY "dbo"."Products"."Id", "dbo"."Products"."PROD_NAME"
                HAVING COUNT(*) > 1
                ORDER BY "dbo"."Products"."PROD_NAME" DESC
                LIMIT 10 OFFSET 5
                """
            ]
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
            ]
        )
    ];

    public static TheoryData<SqlTestCase> TopKeywordData =>
    [
        new SqlTestCase(
            SqlDialectKind.SqlServer,
            [
                """
                SELECT TOP 10 [dbo].[Products].[Id]
                FROM [dbo].[Products]
                """
            ]
        )
    ];

    public static TheoryData<SqlTestCase> OnDuplicateKeyData =>
    [
        new SqlTestCase(
            SqlDialectKind.MySql,
            [
                """
                INSERT INTO `dbo`.`Products` (Id, Price) 
                VALUES (@p0, @p1) 
                ON DUPLICATE KEY UPDATE Price = @p2
                """
            ]
        )
    ];

    public static TheoryData<SqlTestCase> OnConflictData =>
    [
        new SqlTestCase(
            SqlDialectKind.PostgreSql,
            [
                """
                INSERT INTO "dbo"."Products" (Id) 
                VALUES ($1) 
                ON CONFLICT DO NOTHING
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.SqLite,
            [
                """
                INSERT INTO "dbo"."Products" (Id) 
                VALUES (?0) 
                ON CONFLICT DO NOTHING
                """
            ]
        )
    ];

    public static TheoryData<SqlTestCase> WindowFunctionData =>
    [
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

    public static TheoryData<SqlTestCase> CteData =>
    [
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
        )
    ];
}