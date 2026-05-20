using SqlInterpol.Config;
using SqlInterpol.Metadata;
using SqlInterpol.Test.Dialects;
using SqlInterpol.Test.Models;

namespace SqlInterpol.Test;

public class GroupBySubqueryTests
{
    [Theory]
    [MemberData(nameof(GroupBySubqueryData))]
    public void GroupBy_AgainstSubquery(SqlTestCase testCase)
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
        testCase.AssertSql(result.Sql);
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
            SqlDialectKind.Firebird,
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