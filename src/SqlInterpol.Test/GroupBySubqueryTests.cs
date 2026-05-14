using SqlInterpol.Config;
using SqlInterpol.Metadata;
using SqlInterpol.Test.Dialects;
using SqlInterpol.Test.Models;

namespace SqlInterpol.Test;

public class GroupBySubqueryTests
{
    // Local model for the subquery shape
    [SqlTable("Stats")]
    public record StatsModel(int CategoryId, decimal MaxPrice);

    [Theory]
    [MemberData(nameof(GroupBySubqueryData))]
    public void GroupBy_AgainstSubquery_FormatsCorrectly(SqlTestCase testCase)
    {
        // Arrange
        var db = testCase.CreateBuilder();
        
        // Act
        var result = db.Query<StatsModel>(sq =>
            db.Append($$"""
            SELECT CategoryId, COUNT(*)
            FROM (
                SELECT CategoryId, MAX(Price) AS MaxPrice FROM Products GROUP BY CategoryId
            ) AS {{sq.As("stats")}}
            GROUP BY {{sq[x => x.CategoryId]}}
            """))
            .Build();

        // Assert
        Assert.Equal(testCase.ExpectedSql[0], result.Sql);
    }

    public static TheoryData<SqlTestCase> GroupBySubqueryData =>
    [
        new SqlTestCase(
            SqlDialectKind.CustomDb, 
            [
                """
                SELECT CategoryId, COUNT(*)
                FROM (
                    SELECT CategoryId, MAX(Price) AS MaxPrice FROM Products GROUP BY CategoryId
                ) AS <<stats>>
                GROUP BY <<stats>>.<<CategoryId>>
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.MySql, 
            [
                """
                SELECT CategoryId, COUNT(*)
                FROM (
                    SELECT CategoryId, MAX(Price) AS MaxPrice FROM Products GROUP BY CategoryId
                ) AS `stats`
                GROUP BY `stats`.`CategoryId`
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.Oracle, 
            [
                """
                SELECT CategoryId, COUNT(*)
                FROM (
                    SELECT CategoryId, MAX(Price) AS MaxPrice FROM Products GROUP BY CategoryId
                ) AS "stats"
                GROUP BY "stats"."CategoryId"
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.PostgreSql, 
            [
                """
                SELECT CategoryId, COUNT(*)
                FROM (
                    SELECT CategoryId, MAX(Price) AS MaxPrice FROM Products GROUP BY CategoryId
                ) AS "stats"
                GROUP BY "stats"."CategoryId"
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.SqLite, 
            [
                """
                SELECT CategoryId, COUNT(*)
                FROM (
                    SELECT CategoryId, MAX(Price) AS MaxPrice FROM Products GROUP BY CategoryId
                ) AS "stats"
                GROUP BY "stats"."CategoryId"
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.SqlServer, 
            [
                """
                SELECT CategoryId, COUNT(*)
                FROM (
                    SELECT CategoryId, MAX(Price) AS MaxPrice FROM Products GROUP BY CategoryId
                ) AS [stats]
                GROUP BY [stats].[CategoryId]
                """
            ]
        )
    ];
}