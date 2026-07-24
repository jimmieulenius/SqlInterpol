using SqlInterpol.Configuration;
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
        testCase.Action(() => db
            .Entity<StatsModel>(out var stats, "stats")
            .Query(stats, out var statsQuery, () => db.Append($$"""
                SELECT CategoryId, MAX(Price) AS MaxPrice FROM Products GROUP BY CategoryId
                """))
            .Append($$"""
                SELECT CategoryId, COUNT(*)
                FROM {{stats:decl}}
                GROUP BY {{stats.CategoryId}}
                """)
            .Build()
        );

        // Assert
        testCase.Assert();
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
                ) "stats"
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