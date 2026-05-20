using SqlInterpol.Config;
using SqlInterpol.Test.Dialects;
using SqlInterpol.Test.Models;

namespace SqlInterpol.Test;

public class OrderBySubqueryTests
{
    [Theory]
    [MemberData(nameof(OrderBySubqueryData))]
    public void OrderBy_AgainstSubquery_FormatsCorrectly(SqlTestCase testCase)
    {
        // Arrange
        var db = testCase.CreateBuilder();
        
        // Act
        var result = db.Query<StatsModel>(sm =>
            db.Append($$"""
            SELECT *
            FROM
            (
                SELECT
                    CategoryId,
                    MAX(Price) AS MaxPrice
                FROM Products
                GROUP BY CategoryId
            ) AS {{sm.As("stats")}}
            ORDER BY {{sm.OrderBy(x => x.MaxPrice, SqlOrderDirection.Desc)}}
            """))
            .Build();

        // Assert
        testCase.AssertSql(result.Sql);
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
                ) AS "stats"
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