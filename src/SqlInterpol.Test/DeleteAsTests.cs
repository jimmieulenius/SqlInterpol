using SqlInterpol.Test.Dialects;
using SqlInterpol.Test.Models;

namespace SqlInterpol.Test;

public class DeleteAsTests
{
    private const int TargetId = 42;

    [Theory]
    [MemberData(nameof(DeleteWithExplicitAliasData))]
    public void Delete_WithExplicitAlias(SqlTestCase testCase)
    {
        // Arrange
        var db = testCase.CreateBuilder();

        // Act
        testCase.Action(() => db.Entity<OrderModel>(out var o)
            .Append($$""" 
                DELETE FROM {{o}} AS {{"o"}}
                WHERE {{o.Id}} = {{TargetId}}
                """)
            .Build()
        );

        // Assert
        testCase.Assert();
    }

    [Theory]
    [MemberData(nameof(DeleteWithoutAliasData))]
    public void Delete_WithoutAlias_StripsAutoAlias(SqlTestCase testCase)
    {
        // Arrange
        var db = testCase.CreateBuilder();
        db.Context.Options.EntityAutoAliasing = true; // Even with auto-aliasing enabled, it strips safely!

        // Act
        testCase.Action(() => db.Entity<OrderModel>(out var o)
            .Append($$""" 
                DELETE FROM {{o}}
                WHERE {{o.Id}} = {{TargetId}}
                """)
            .Build()
        );

        // Assert
        testCase.Assert();
    }

    public static TheoryData<SqlTestCase> DeleteWithExplicitAliasData
    {
        get
        {
            object?[] parameters = [TargetId];

            return
            [
                new SqlTestCase(SqlDialectKind.CustomDb, typeof(SqlDialectException)),
                new SqlTestCase(SqlDialectKind.Firebird, typeof(SqlDialectException)),
                new SqlTestCase(SqlDialectKind.MySql, 
                    expectedSql: [
                        """
                        DELETE `o` FROM `dbo`.`Orders` AS `o`
                        WHERE `o`.`Id` = @p0
                        """
                    ],
                    expectedParameters: parameters
                ),
                new SqlTestCase(SqlDialectKind.Oracle, typeof(SqlDialectException)),
                new SqlTestCase(SqlDialectKind.PostgreSql, 
                    expectedSql: [
                        """
                        DELETE FROM "dbo"."Orders" AS "o"
                        WHERE "o"."Id" = $1
                        """
                    ],
                    expectedParameters: parameters
                ),
                new SqlTestCase(SqlDialectKind.SqLite, typeof(SqlDialectException)),
                new SqlTestCase(SqlDialectKind.SqlServer, 
                    expectedSql: [
                        """
                        DELETE [o] FROM [dbo].[Orders] AS [o]
                        WHERE [o].[Id] = @p0
                        """
                    ],
                    expectedParameters: parameters
                )
            ];
        }
    }

    public static TheoryData<SqlTestCase> DeleteWithoutAliasData
    {
        get
        {
            object?[] parameters = [TargetId];

            return
            [
                // ALL Dialects safely strip the alias because the user didn't explicitly ask for it!
                new SqlTestCase(SqlDialectKind.CustomDb, [
                    """
                    DELETE FROM <<dbo>>.<<Orders>>
                    WHERE <<dbo>>.<<Orders>>.<<Id>> = !!100
                    """], expectedParameters: parameters),

                new SqlTestCase(SqlDialectKind.Firebird, [
                    """
                    DELETE FROM "dbo"."Orders"
                    WHERE "dbo"."Orders"."Id" = @p0
                    """], expectedParameters: parameters),
                
                new SqlTestCase(SqlDialectKind.MySql, [
                    """
                    DELETE FROM `dbo`.`Orders`
                    WHERE `dbo`.`Orders`.`Id` = @p0
                    """], expectedParameters: parameters),

                new SqlTestCase(SqlDialectKind.Oracle, [
                    """
                    DELETE FROM "dbo"."Orders"
                    WHERE "dbo"."Orders"."Id" = :0
                    """], expectedParameters: parameters),

                new SqlTestCase(SqlDialectKind.PostgreSql, [
                    """
                    DELETE FROM "dbo"."Orders"
                    WHERE "dbo"."Orders"."Id" = $1
                    """], expectedParameters: parameters),

                new SqlTestCase(SqlDialectKind.SqLite, [
                    """
                    DELETE FROM "dbo"."Orders"
                    WHERE "dbo"."Orders"."Id" = @p1
                    """], expectedParameters: parameters),

                new SqlTestCase(SqlDialectKind.SqlServer, [
                    """
                    DELETE FROM [dbo].[Orders]
                    WHERE [dbo].[Orders].[Id] = @p0
                    """], expectedParameters: parameters),
            ];
        }
    }
}