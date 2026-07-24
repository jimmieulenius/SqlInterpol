using SqlInterpol.Configuration;
using SqlInterpol.Test.Dialects;
using SqlInterpol.Test.Models;

namespace SqlInterpol.Test;

public class UpdateAsTests
{
    [Theory]
    [MemberData(nameof(UpdateSetWithAliasData))]
    public void Update_WithExplicitAlias(SqlTestCase testCase)
    {
        // Arrange
        var db = testCase.CreateBuilder();
        var updateDto = new { Status = "Shipped", Total = 99.99m };
        
        // Act
        testCase.Action(() => db
            .Entity<OrderModel>(out var o)
            .Append($$"""
                UPDATE {{o}} AS {{"ord"}}
                SET {{updateDto}}
                WHERE {{o.Id}} = 1
                """)
            .Build()
        );

        // Assert
        testCase.Assert();
    }

    public static TheoryData<SqlTestCase> UpdateSetWithAliasData
    {
        get
        {
            object?[] expectedParams = ["Shipped", 99.99m];

            return
            [
                new SqlTestCase(SqlDialectKind.CustomDb, typeof(SqlDialectException)),
                new SqlTestCase(SqlDialectKind.Firebird, typeof(SqlDialectException)),
                new SqlTestCase(
                    SqlDialectKind.MySql,
                    [
                        """
                        UPDATE `ord`, `dbo`.`Orders` AS `ord`
                        SET `order_status` = @p0, `Total` = @p1
                        WHERE `ord`.`Id` = 1
                        """
                    ],
                    expectedParameters: expectedParams
                ),
                new SqlTestCase(SqlDialectKind.Oracle, typeof(SqlDialectException)),
                new SqlTestCase(
                    SqlDialectKind.PostgreSql,
                    [
                        """
                        UPDATE "dbo"."Orders" AS "ord"
                        SET "order_status" = $1, "Total" = $2
                        WHERE "ord"."Id" = 1
                        """
                    ],
                    expectedParameters: expectedParams
                ),
                new SqlTestCase(SqlDialectKind.SqLite, typeof(SqlDialectException)),
                new SqlTestCase(
                    SqlDialectKind.SqlServer,
                    [
                        """
                        UPDATE [ord]
                        SET [order_status] = @p0, [Total] = @p1
                        FROM [dbo].[Orders] AS [ord]
                        WHERE [ord].[Id] = 1
                        """
                    ],
                    expectedParameters: expectedParams
                )
            ];
        }
    }
}