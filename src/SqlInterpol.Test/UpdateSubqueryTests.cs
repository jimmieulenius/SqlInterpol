using SqlInterpol.Test.Dialects;
using SqlInterpol.Test.Models;

namespace SqlInterpol.Test;

public class UpdateSubqueryTests
{
    [Theory]
    [MemberData(nameof(UpdateSubqueryData))]
    public void Update_AgainstRawSubquery(SqlTestCase testCase)
    {
        // Arrange
        var db = testCase.CreateBuilder();
        var updateDto = new { MaxPrice = 99.99m };
        
        // Act
        var result = db.Query<OrderStatsModel>(sq =>
            db.Append($$"""
            UPDATE (
                SELECT CategoryId, MAX(Price) AS max_price FROM Products GROUP BY CategoryId
            ) AS {{sq.As("stats")}}
            SET {{updateDto}}
            WHERE {{sq[x => x.CategoryId]}} = 5
            """))
            .Build();

        // Assert
        testCase.AssertSql(result.Sql);
        
        Assert.Equal(99.99m, result.Parameters.ElementAt(0).Value);
    }

    [Theory]
    [MemberData(nameof(UpdateTypeSafeSubqueryData))]
    public void Update_AgainstTypeSafeSubquery(SqlTestCase testCase)
    {
        // Arrange
        var db = testCase.CreateBuilder();
        var updateDto = new { MaxPrice = 99.99m };

        // Act
        var result = db
            .Entity<OrderStatsModel>()
            .Entity<Product>(alias: "p")
            .Query((stats, p) =>
            db.Append($$"""
            UPDATE (
                SELECT 
                    {{p[x => x.CategoryId]}} AS {{stats[x => x.CategoryId]}}, 
                    MAX({{p[x => x.Price]}}) AS {{stats[x => x.MaxPrice]}} 
                FROM {{p}} 
                GROUP BY {{p[x => x.CategoryId]}}
            ) AS {{stats.As("stats")}}
            SET {{updateDto}}
            WHERE {{stats[x => x.CategoryId]}} = 5
            """))
            .Build();

        // Assert
        testCase.AssertSql(result.Sql);
        Assert.Equal(99.99m, result.Parameters.ElementAt(0).Value);
    }

    public static TheoryData<SqlTestCase> UpdateSubqueryData =>
    [
        new SqlTestCase(
            SqlDialectKind.CustomDb, 
            [
                """
                UPDATE (
                    SELECT CategoryId, MAX(Price) AS max_price FROM Products GROUP BY CategoryId
                ) AS <<stats>>
                SET <<max_price>> = !!100
                WHERE <<stats>>.<<CategoryId>> = 5
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.MySql, 
            [
                """
                UPDATE (
                    SELECT CategoryId, MAX(Price) AS max_price FROM Products GROUP BY CategoryId
                ) AS `stats`
                SET `max_price` = @p0
                WHERE `stats`.`CategoryId` = 5
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.Oracle, 
            [
                """
                UPDATE (
                    SELECT CategoryId, MAX(Price) AS max_price FROM Products GROUP BY CategoryId
                ) AS "stats"
                SET "max_price" = :0
                WHERE "stats"."CategoryId" = 5
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.PostgreSql, 
            [
                """
                UPDATE (
                    SELECT CategoryId, MAX(Price) AS max_price FROM Products GROUP BY CategoryId
                ) AS "stats"
                SET "max_price" = $1
                WHERE "stats"."CategoryId" = 5
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.SqLite, 
            [
                """
                UPDATE (
                    SELECT CategoryId, MAX(Price) AS max_price FROM Products GROUP BY CategoryId
                ) AS "stats"
                SET "max_price" = @p1
                WHERE "stats"."CategoryId" = 5
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.SqlServer, 
            [
                """
                UPDATE (
                    SELECT CategoryId, MAX(Price) AS max_price FROM Products GROUP BY CategoryId
                ) AS [stats]
                SET [max_price] = @p0
                WHERE [stats].[CategoryId] = 5
                """
            ]
        )
    ];

    public static TheoryData<SqlTestCase> UpdateTypeSafeSubqueryData =>
    [
        new SqlTestCase(
            SqlDialectKind.CustomDb, 
            [
                """
                UPDATE (
                    SELECT 
                        <<p>>.<<CategoryId>> AS <<CategoryId>>, 
                        MAX(<<p>>.<<Price>>) AS <<MaxPrice>> 
                    FROM <<dbo>>.<<Products>> AS <<p>> 
                    GROUP BY <<p>>.<<CategoryId>>
                ) AS <<stats>>
                SET <<max_price>> = !!100
                WHERE <<stats>>.<<CategoryId>> = 5
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.MySql, 
            [
                """
                UPDATE (
                    SELECT 
                        `p`.`CategoryId` AS `CategoryId`, 
                        MAX(`p`.`Price`) AS `MaxPrice` 
                    FROM `dbo`.`Products` AS `p` 
                    GROUP BY `p`.`CategoryId`
                ) AS `stats`
                SET `max_price` = @p0
                WHERE `stats`.`CategoryId` = 5
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.Oracle, 
            [
                """
                UPDATE (
                    SELECT 
                        "p"."CategoryId" AS "CategoryId", 
                        MAX("p"."Price") AS "MaxPrice" 
                    FROM "dbo"."Products" AS "p" 
                    GROUP BY "p"."CategoryId"
                ) AS "stats"
                SET "max_price" = :0
                WHERE "stats"."CategoryId" = 5
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.PostgreSql, 
            [
                """
                UPDATE (
                    SELECT 
                        "p"."CategoryId" AS "CategoryId", 
                        MAX("p"."Price") AS "MaxPrice" 
                    FROM "dbo"."Products" AS "p" 
                    GROUP BY "p"."CategoryId"
                ) AS "stats"
                SET "max_price" = $1
                WHERE "stats"."CategoryId" = 5
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.SqLite, 
            [
                """
                UPDATE (
                    SELECT 
                        "p"."CategoryId" AS "CategoryId", 
                        MAX("p"."Price") AS "MaxPrice" 
                    FROM "dbo"."Products" AS "p" 
                    GROUP BY "p"."CategoryId"
                ) AS "stats"
                SET "max_price" = @p1
                WHERE "stats"."CategoryId" = 5
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.SqlServer, 
            [
                """
                UPDATE (
                    SELECT 
                        [p].[CategoryId] AS [CategoryId], 
                        MAX([p].[Price]) AS [MaxPrice] 
                    FROM [dbo].[Products] AS [p] 
                    GROUP BY [p].[CategoryId]
                ) AS [stats]
                SET [max_price] = @p0
                WHERE [stats].[CategoryId] = 5
                """
            ]
        )
    ];
}