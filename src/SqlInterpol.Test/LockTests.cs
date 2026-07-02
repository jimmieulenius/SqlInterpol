using SqlInterpol.Test.Dialects;
using SqlInterpol.Test.Models;

namespace SqlInterpol.Test;

public class LockTests
{
    [Theory]
    [MemberData(nameof(SelectWithForUpdateData))]
    public void Select_WithForUpdate(SqlTestCase testCase)
    {
        // Arrange
        var db = testCase.CreateBuilder();
        int id = 5;

        // Act - Zero-allocation properties and fluent target tracking
        testCase.Action(() => db.Entity<Product>(out var p)
            .Append($$"""
            SELECT {{p.Id}}, {{p.Name}}
            FROM {{p}} FOR UPDATE
            WHERE {{p.Id}} = {{id}}
            """)
            .Build()
        );

        // Assert - Handles both string verification and exception assertions natively
        testCase.Assert();
    }

    [Theory]
    [MemberData(nameof(SelectWithForShareData))]
    public void Select_WithForShare(SqlTestCase testCase)
    {
        // Arrange
        var db = testCase.CreateBuilder();
        int id = 5;

        // Act
        testCase.Action(() => db.Entity<Product>(out var p)
            .Append($$"""
            SELECT {{p.Id}}, {{p.Name}}
            FROM {{p}} FOR SHARE
            WHERE {{p.Id}} = {{id}}
            """)
            .Build()
        );

        // Assert
        testCase.Assert();
    }

    public static TheoryData<SqlTestCase> SelectWithForUpdateData
    {
        get
        {
            object?[] expectedParams = [5];

            return
            [
                new SqlTestCase(
                    SqlDialectKind.CustomDb,
                    expectedExceptionType: typeof(SqlDialectException),
                    expectedExceptionMessage: $"Dialect capabilities validation failed:{Environment.NewLine}- 'FOR UPDATE' is not supported by CustomDb."
                ),
                new SqlTestCase(
                    SqlDialectKind.Firebird,
                    [
                        """
                        SELECT "dbo"."Products"."Id", "dbo"."Products"."PROD_NAME"
                        FROM "dbo"."Products"
                        WHERE "dbo"."Products"."Id" = @p0
                        WITH LOCK
                        """
                    ],
                    expectedParameters: expectedParams
                ),
                new SqlTestCase(
                    SqlDialectKind.MySql,
                    [
                        """
                        SELECT `dbo`.`Products`.`Id`, `dbo`.`Products`.`PROD_NAME`
                        FROM `dbo`.`Products`
                        WHERE `dbo`.`Products`.`Id` = @p0
                        FOR UPDATE
                        """
                    ],
                    expectedParameters: expectedParams
                ),
                new SqlTestCase(
                    SqlDialectKind.Oracle,
                    [
                        """
                        SELECT "dbo"."Products"."Id", "dbo"."Products"."PROD_NAME"
                        FROM "dbo"."Products"
                        WHERE "dbo"."Products"."Id" = :0
                        FOR UPDATE
                        """
                    ],
                    expectedParameters: expectedParams
                ),
                new SqlTestCase(
                    SqlDialectKind.PostgreSql,
                    [
                        """
                        SELECT "dbo"."Products"."Id", "dbo"."Products"."PROD_NAME"
                        FROM "dbo"."Products"
                        WHERE "dbo"."Products"."Id" = $1
                        FOR UPDATE
                        """
                    ],
                    expectedParameters: expectedParams
                ),
                new SqlTestCase(
                    SqlDialectKind.SqLite,
                    expectedExceptionType: typeof(SqlDialectException),
                    expectedExceptionMessage: $"Dialect capabilities validation failed:{Environment.NewLine}- 'FOR UPDATE' is not supported by SqLite."
                ),
                new SqlTestCase(
                    SqlDialectKind.SqlServer,
                    [
                        """
                        SELECT [dbo].[Products].[Id], [dbo].[Products].[PROD_NAME]
                        FROM [dbo].[Products] WITH (UPDLOCK)
                        WHERE [dbo].[Products].[Id] = @p0
                        """
                    ],
                    expectedParameters: expectedParams
                )
            ];
        }
    }

    public static TheoryData<SqlTestCase> SelectWithForShareData
    {
        get
        {
            object?[] expectedParams = [5];

            return
            [
                new SqlTestCase(
                    SqlDialectKind.CustomDb,
                    expectedExceptionType: typeof(SqlDialectException),
                    expectedExceptionMessage: $"Dialect capabilities validation failed:{Environment.NewLine}- 'FOR SHARE' is not supported by CustomDb."
                ),
                new SqlTestCase(
                    SqlDialectKind.Firebird,
                    expectedExceptionType: typeof(SqlDialectException),
                    expectedExceptionMessage: $"Dialect capabilities validation failed:{Environment.NewLine}- 'FOR SHARE' is not supported by Firebird."
                ),
                new SqlTestCase(
                    SqlDialectKind.MySql,
                    [
                        """
                        SELECT `dbo`.`Products`.`Id`, `dbo`.`Products`.`PROD_NAME`
                        FROM `dbo`.`Products`
                        WHERE `dbo`.`Products`.`Id` = @p0
                        FOR SHARE
                        """
                    ],
                    expectedParameters: expectedParams
                ),
                new SqlTestCase(
                    SqlDialectKind.Oracle,
                    expectedExceptionType: typeof(SqlDialectException),
                    expectedExceptionMessage: $"Dialect capabilities validation failed:{Environment.NewLine}- 'FOR SHARE' is not supported by Oracle."
                ),
                new SqlTestCase(
                    SqlDialectKind.PostgreSql,
                    [
                        """
                        SELECT "dbo"."Products"."Id", "dbo"."Products"."PROD_NAME"
                        FROM "dbo"."Products"
                        WHERE "dbo"."Products"."Id" = $1
                        FOR SHARE
                        """
                    ],
                    expectedParameters: expectedParams
                ),
                new SqlTestCase(
                    SqlDialectKind.SqLite,
                    expectedExceptionType: typeof(SqlDialectException),
                    expectedExceptionMessage: $"Dialect capabilities validation failed:{Environment.NewLine}- 'FOR SHARE' is not supported by SqLite."
                ),
                new SqlTestCase(
                    SqlDialectKind.SqlServer,
                    [
                        """
                        SELECT [dbo].[Products].[Id], [dbo].[Products].[PROD_NAME]
                        FROM [dbo].[Products] WITH (ROWLOCK, HOLDLOCK)
                        WHERE [dbo].[Products].[Id] = @p0
                        """
                    ],
                    expectedParameters: expectedParams
                )
            ];
        }
    }
}