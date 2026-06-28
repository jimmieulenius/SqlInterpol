using SqlInterpol.Test.Dialects;
using SqlInterpol.Test.Models;

namespace SqlInterpol.Test;

public class UpdateSubqueryTests
{
    private const decimal TargetMaxPrice = 99.99m;

    [Theory]
    [MemberData(nameof(UpdateSubqueryData))]
    public void Update_AgainstRawSubquery(SqlTestCase testCase)
    {
        // Arrange
        var db = testCase.CreateBuilder();
        var updateDto = new { MaxPrice = TargetMaxPrice };
        
        // Act
        testCase.Action(() => db
            .Entity<OrderStatsModel>(out var stats)
            .Append($$"""
                UPDATE (
                    {{db.Subquery(stats, () => db.Append($$"""
                        SELECT CategoryId, MAX(Price) AS max_price FROM Products GROUP BY CategoryId
                        """))}}
                ) AS {{"stats"}}
                SET {{updateDto}}
                WHERE {{stats.CategoryId}} = 5
                """)
            .Build()
        );

        // Assert
        testCase.Assert();
    }

    [Theory]
    [MemberData(nameof(UpdateTypeSafeSubqueryData))]
    public void Update_AgainstTypeSafeSubquery(SqlTestCase testCase)
    {
        // Arrange
        var db = testCase.CreateBuilder();
        var updateDto = new { MaxPrice = TargetMaxPrice };

        // Act
        testCase.Action(() => db
            .Entity<OrderStatsModel>(out var stats)
            .Entity<Product>(out var p)
            .Append($$"""
                UPDATE (
                    {{db.Subquery(stats, () => db.Append($$"""
                        SELECT
                            {{p.CategoryId}} AS {{stats.CategoryId}},
                            MAX({{p.Price}}) AS {{stats.MaxPrice}}
                        FROM {{p}} AS {{"p"}}
                        GROUP BY {{p.CategoryId}}
                        """))}}
                ) AS {{"stats"}}
                SET {{updateDto}}
                WHERE {{stats.CategoryId}} = 5
                """)
            .Build()
        );

        // Assert
        testCase.Assert();
    }

    public static TheoryData<SqlTestCase> UpdateSubqueryData
    {
        get
        {
            object?[] expectedParams = [TargetMaxPrice];

            return
            [
                new SqlTestCase(
                    SqlDialectKind.CustomDb,
                    [
                        """
                        UPDATE (SELECT CategoryId, MAX(Price) AS max_price FROM Products GROUP BY CategoryId) AS <<stats>>
                        SET <<max_price>> = !!100
                        WHERE <<stats>>.<<CategoryId>> = 5
                        """
                    ],
                    expectedParameters: expectedParams
                ),
                new SqlTestCase(
                    SqlDialectKind.MySql,
                    [
                        """
                        UPDATE (SELECT CategoryId, MAX(Price) AS max_price FROM Products GROUP BY CategoryId) AS `stats`
                        SET `max_price` = @p0
                        WHERE `stats`.`CategoryId` = 5
                        """
                    ],
                    expectedParameters: expectedParams
                ),
                new SqlTestCase(
                    SqlDialectKind.Oracle,
                    [
                        """
                        UPDATE (SELECT CategoryId, MAX(Price) AS max_price FROM Products GROUP BY CategoryId) "stats"
                        SET "max_price" = :0
                        WHERE "stats"."CategoryId" = 5
                        """
                    ],
                    expectedParameters: expectedParams
                ),
                new SqlTestCase(
                    SqlDialectKind.PostgreSql,
                    [
                        """
                        WITH "stats" AS (
                            SELECT CategoryId, MAX(Price) AS max_price FROM Products GROUP BY CategoryId
                        )
                        UPDATE "stats"
                        SET "max_price" = $1
                        WHERE "stats"."CategoryId" = 5
                        """
                    ],
                    expectedParameters: expectedParams
                ),
                new SqlTestCase(
                    SqlDialectKind.SqLite,
                    [
                        """
                        WITH "stats" AS (
                            SELECT CategoryId, MAX(Price) AS max_price FROM Products GROUP BY CategoryId
                        )
                        UPDATE "stats"
                        SET "max_price" = @p1
                        WHERE "stats"."CategoryId" = 5
                        """
                    ],
                    expectedParameters: expectedParams
                ),
                new SqlTestCase(
                    SqlDialectKind.SqlServer,
                    [
                        """
                        WITH [stats] AS (
                            SELECT CategoryId, MAX(Price) AS max_price FROM Products GROUP BY CategoryId
                        )
                        UPDATE [stats]
                        SET [max_price] = @p0
                        WHERE [stats].[CategoryId] = 5
                        """
                    ],
                    expectedParameters: expectedParams
                )
            ];
        }
    }

    public static TheoryData<SqlTestCase> UpdateTypeSafeSubqueryData
    {
        get
        {
            object?[] expectedParams = [TargetMaxPrice];

            return
            [
                new SqlTestCase(
                    SqlDialectKind.CustomDb,
                    [
                        """
                        UPDATE (SELECT <<p>>.<<CategoryId>> AS <<CategoryId>>, MAX(<<p>>.<<Price>>) AS <<MaxPrice>> FROM <<dbo>>.<<Products>> AS <<p>> GROUP BY <<p>>.<<CategoryId>>) AS <<stats>>
                        SET <<max_price>> = !!100
                        WHERE <<stats>>.<<CategoryId>> = 5
                        """
                    ],
                    expectedParameters: expectedParams
                ),
                new SqlTestCase(
                    SqlDialectKind.MySql,
                    [
                        """
                        UPDATE (SELECT `p`.`CategoryId` AS `CategoryId`, MAX(`p`.`Price`) AS `MaxPrice` FROM `dbo`.`Products` AS `p` GROUP BY `p`.`CategoryId`) AS `stats`
                        SET `max_price` = @p0
                        WHERE `stats`.`CategoryId` = 5
                        """
                    ],
                    expectedParameters: expectedParams
                ),
                new SqlTestCase(
                    SqlDialectKind.Oracle,
                    [
                        """
                        UPDATE (SELECT "p"."CategoryId" AS "CategoryId", MAX("p"."Price") AS "MaxPrice" FROM "dbo"."Products" "p" GROUP BY "p"."CategoryId") "stats"
                        SET "max_price" = :0
                        WHERE "stats"."CategoryId" = 5
                        """
                    ],
                    expectedParameters: expectedParams
                ),
                new SqlTestCase(
                    SqlDialectKind.PostgreSql,
                    [
                        """
                        WITH "stats" AS (
                            SELECT
                                "p"."CategoryId" AS "CategoryId",
                                MAX("p"."Price") AS "MaxPrice"
                            FROM "dbo"."Products" AS "p"
                            GROUP BY "p"."CategoryId"
                        )
                        UPDATE "stats"
                        SET "max_price" = $1
                        WHERE "stats"."CategoryId" = 5
                        """
                    ],
                    expectedParameters: expectedParams
                ),
                new SqlTestCase(
                    SqlDialectKind.SqLite,
                    [
                        """
                        WITH "stats" AS (
                            SELECT
                                "p"."CategoryId" AS "CategoryId",
                                MAX("p"."Price") AS "MaxPrice"
                            FROM "dbo"."Products" AS "p"
                            GROUP BY "p"."CategoryId"
                        )
                        UPDATE "stats"
                        SET "max_price" = @p1
                        WHERE "stats"."CategoryId" = 5
                        """
                    ],
                    expectedParameters: expectedParams
                ),
                new SqlTestCase(
                    SqlDialectKind.SqlServer,
                    [
                        """
                        WITH [stats] AS (
                            SELECT
                                [p].[CategoryId] AS [CategoryId],
                                MAX([p].[Price]) AS [MaxPrice]
                            FROM [dbo].[Products] AS [p]
                            GROUP BY [p].[CategoryId]
                        )
                        UPDATE [stats]
                        SET [max_price] = @p0
                        WHERE [stats].[CategoryId] = 5
                        """
                    ],
                    expectedParameters: expectedParams
                )
            ];
        }
    }
}