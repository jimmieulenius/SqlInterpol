using SqlInterpol.Config;
using SqlInterpol.Test.Dialects;
using SqlInterpol.Test.Models;
using SqlInterpol.Test.Parsing;

namespace SqlInterpol.Test;

public class CustomParserTests
{
    private static readonly SqlInterpolOptions _options = new() { Parser = new CustomSqlParser() };

    [Theory]
    [MemberData(nameof(CustomParserData))]
    public void CustomParser(SqlTestCase testCase)
    {
        // Arrange
        var activeIds = new List<int> { 10, 20, 30 };
        var db = testCase.CreateBuilder(_options with { Dialect = testCase.Dialect });
        db.Append($$"""
            SELECT *
            FROM Users
            WHERE RoleId CUSTOM_IN {{activeIds}}
            """);

        // Act
        var result = db.Build();

        // Assert
        testCase.AssertSql(result.Sql);
    }

    public static TheoryData<SqlTestCase> CustomParserData =>
    [
        new SqlTestCase(
            SqlDialectKind.CustomDb,
            [
                """
                SELECT *
                FROM Users
                WHERE RoleId CUSTOM_IN (!!0, !!1, !!2)
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.MySql,
            [
                """
                SELECT *
                FROM Users
                WHERE RoleId CUSTOM_IN (@p0, @p1, @p2)
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.Oracle,
            [
                """
                SELECT *
                FROM Users
                WHERE RoleId CUSTOM_IN (:0, :1, :2)
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.PostgreSql,
            [
                """
                SELECT *
                FROM Users
                WHERE RoleId CUSTOM_IN ($0, $1, $2)
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.SqLite,
            [
                """
                SELECT *
                FROM Users
                WHERE RoleId CUSTOM_IN (?0, ?1, ?2)
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.SqlServer,
            [
                """
                SELECT *
                FROM Users
                WHERE RoleId CUSTOM_IN (@p0, @p1, @p2)
                """
            ]
        )
    ];
}