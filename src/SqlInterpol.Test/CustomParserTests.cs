using SqlInterpol.Configuration;
using SqlInterpol.Test.Dialects;
using SqlInterpol.Test.Models;
using SqlInterpol.Test.Parsing;

namespace SqlInterpol.Test;

public class CustomParserTests
{
    private static readonly SqlInterpolOptions _options = new() { Preprocessor = new CustomSqlPreprocessor() };
    
    private static readonly List<int> ActiveIds = [10, 20, 30];

    [Theory]
    [MemberData(nameof(CustomParserData))]
    public void CustomParser(SqlTestCase testCase)
    {
        // Arrange
        var db = testCase.CreateBuilder(_options with { Dialect = testCase.Dialect });

        // Act
        testCase.Action(() => db.Append($$"""
            SELECT *
            FROM Users
            WHERE RoleId CUSTOM_IN {{ActiveIds}}
            """).Build()
        );

        // Assert
        testCase.Assert();
    }

    public static TheoryData<SqlTestCase> CustomParserData
    {
        get
        {
            object?[] expectedParams = [ActiveIds[0], ActiveIds[1], ActiveIds[2]];

            return
            [
                new SqlTestCase(
                    SqlDialectKind.CustomDb,
                    [
                        """
                        SELECT *
                        FROM Users
                        WHERE RoleId CUSTOM_IN (!!0, !!1, !!2)
                        """
                    ],
                    expectedParameters: expectedParams
                ),
                new SqlTestCase(
                    SqlDialectKind.MySql,
                    [
                        """
                        SELECT *
                        FROM Users
                        WHERE RoleId CUSTOM_IN (@p0, @p1, @p2)
                        """
                    ],
                    expectedParameters: expectedParams
                ),
                new SqlTestCase(
                    SqlDialectKind.Oracle,
                    [
                        """
                        SELECT *
                        FROM Users
                        WHERE RoleId CUSTOM_IN (:0, :1, :2)
                        """
                    ],
                    expectedParameters: expectedParams
                ),
                new SqlTestCase(
                    SqlDialectKind.PostgreSql,
                    [
                        """
                        SELECT *
                        FROM Users
                        WHERE RoleId CUSTOM_IN ($0, $1, $2)
                        """
                    ],
                    expectedParameters: expectedParams
                ),
                new SqlTestCase(
                    SqlDialectKind.SqLite,
                    [
                        """
                        SELECT *
                        FROM Users
                        WHERE RoleId CUSTOM_IN (@p0, @p1, @p2)
                        """
                    ],
                    expectedParameters: expectedParams
                ),
                new SqlTestCase(
                    SqlDialectKind.SqlServer,
                    [
                        """
                        SELECT *
                        FROM Users
                        WHERE RoleId CUSTOM_IN (@p0, @p1, @p2)
                        """
                    ],
                    expectedParameters: expectedParams
                )
            ];
        }
    }
}