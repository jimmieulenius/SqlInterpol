using SqlInterpol.Configuration;
using SqlInterpol.Test.Dialects;
using SqlInterpol.Test.Models;

namespace SqlInterpol.Test;

public class OrderBySubqueryTests
{
    [Theory]
    [MemberData(nameof(OrderBySubqueryData))]
    public void OrderBy_AgainstSubquery(SqlTestCase testCase)
    {
        // Arrange
        var db = testCase.CreateBuilder();
        
        // Act
        testCase.Action(() => db.Entity<StatsModel>(out var stats, "stats")
            .Append($$"""
            SELECT *
            FROM
            (
                SELECT
                    CategoryId,
                    MAX(Price) AS MaxPrice
                FROM Products
                GROUP BY CategoryId
            ) AS {{stats:alias}}
            ORDER BY {{stats.MaxPrice}} DESC
            """)
            .Build()
        );

        // Assert
        testCase.Assert();
    }

    public static TheoryData<SqlTestCase> OrderBySubqueryData =>
    [
        new SqlTestCase(
            SqlDialectKind.CustomDb, 
            [
                """
                SELECT *
                FROM
                (
                    SELECT
                        CategoryId,
                        MAX(Price) AS MaxPrice
                    FROM Products
                    GROUP BY CategoryId
                ) AS <<stats>>
                ORDER BY <<stats>>.<<MaxPrice>> DESC
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.Firebird,
            [
                """
                SELECT *
                FROM
                (
                    SELECT
                        CategoryId,
                        MAX(Price) AS MaxPrice
                    FROM Products
                    GROUP BY CategoryId
                ) AS "stats"
                ORDER BY "stats"."MaxPrice" DESC
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.MySql, 
            [
                """
                SELECT *
                FROM
                (
                    SELECT
                        CategoryId,
                        MAX(Price) AS MaxPrice
                    FROM Products
                    GROUP BY CategoryId
                ) AS `stats`
                ORDER BY `stats`.`MaxPrice` DESC
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.Oracle, 
            [
                """
                SELECT *
                FROM
                (
                    SELECT
                        CategoryId,
                        MAX(Price) AS MaxPrice
                    FROM Products
                    GROUP BY CategoryId
                ) "stats"
                ORDER BY "stats"."MaxPrice" DESC
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.PostgreSql, 
            [
                """
                SELECT *
                FROM
                (
                    SELECT
                        CategoryId,
                        MAX(Price) AS MaxPrice
                    FROM Products
                    GROUP BY CategoryId
                ) AS "stats"
                ORDER BY "stats"."MaxPrice" DESC
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.SqLite, 
            [
                """
                SELECT *
                FROM
                (
                    SELECT
                        CategoryId,
                        MAX(Price) AS MaxPrice
                    FROM Products
                    GROUP BY CategoryId
                ) AS "stats"
                ORDER BY "stats"."MaxPrice" DESC
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.SqlServer,
            [
                """
                SELECT *
                FROM
                (
                    SELECT
                        CategoryId,
                        MAX(Price) AS MaxPrice
                    FROM Products
                    GROUP BY CategoryId
                ) AS [stats]
                ORDER BY [stats].[MaxPrice] DESC
                """
            ]
        )
    ];
}