using SqlInterpol.Configuration;
using SqlInterpol.Test.Dialects;
using SqlInterpol.Test.Models;

namespace SqlInterpol.Test;

public class DeleteSubqueryTests
{
    private const string Status = "Cancelled";

    [Theory]
    [MemberData(nameof(Delete_WithSubqueryData))]
    public void Delete_WithSubquery(SqlTestCase testCase)
    {
        // Arrange
        var db = testCase.CreateBuilder();

        // Act
        testCase.Action(() => db
            .Entity<OrderLine>(out var l)
            .Entity<OrderModel>(out var o)
            .Append($$"""
                DELETE FROM {{l}}
                WHERE {{l.OrderId}} IN (
                    SELECT {{o.Id}}
                    FROM {{o}}
                    WHERE {{o.Status}} = {{Status}}
                )
                """)
            .Build()
        );

        // Assert
        testCase.Assert();
    }

    public static TheoryData<SqlTestCase> Delete_WithSubqueryData
    {
        get
        {
            object?[] expectedParams = [Status];

            return
            [
                new SqlTestCase(
                    SqlDialectKind.CustomDb,
                    [
                        """
                        DELETE FROM <<OrderLine>>
                        WHERE <<OrderLine>>.<<OrderId>> IN (
                            SELECT <<dbo>>.<<Orders>>.<<Id>>
                            FROM <<dbo>>.<<Orders>>
                            WHERE <<dbo>>.<<Orders>>.<<order_status>> = !!100
                        )
                        """
                    ],
                    expectedParameters: expectedParams
                ),
                new SqlTestCase(
                    SqlDialectKind.Firebird,
                    [
                        """
                        DELETE FROM "OrderLine"
                        WHERE "OrderLine"."OrderId" IN (
                            SELECT "dbo"."Orders"."Id"
                            FROM "dbo"."Orders"
                            WHERE "dbo"."Orders"."order_status" = @p0
                        )
                        """
                    ],
                    expectedParameters: expectedParams
                ),
                new SqlTestCase(
                    SqlDialectKind.MySql,
                    [
                        """
                        DELETE FROM `OrderLine`
                        WHERE `OrderLine`.`OrderId` IN (
                            SELECT `dbo`.`Orders`.`Id`
                            FROM `dbo`.`Orders`
                            WHERE `dbo`.`Orders`.`order_status` = @p0
                        )
                        """
                    ],
                    expectedParameters: expectedParams
                ),
                new SqlTestCase(
                    SqlDialectKind.Oracle,
                    [
                        """
                        DELETE FROM "OrderLine"
                        WHERE "OrderLine"."OrderId" IN (
                            SELECT "dbo"."Orders"."Id"
                            FROM "dbo"."Orders"
                            WHERE "dbo"."Orders"."order_status" = :0
                        )
                        """
                    ],
                    expectedParameters: expectedParams
                ),
                new SqlTestCase(
                    SqlDialectKind.PostgreSql,
                    [
                        """
                        DELETE FROM "OrderLine"
                        WHERE "OrderLine"."OrderId" IN (
                            SELECT "dbo"."Orders"."Id"
                            FROM "dbo"."Orders"
                            WHERE "dbo"."Orders"."order_status" = $1
                        )
                        """
                    ],
                    expectedParameters: expectedParams
                ),
                new SqlTestCase(
                    SqlDialectKind.SqLite,
                    [
                        """
                        DELETE FROM "OrderLine"
                        WHERE "OrderLine"."OrderId" IN (
                            SELECT "dbo"."Orders"."Id"
                            FROM "dbo"."Orders"
                            WHERE "dbo"."Orders"."order_status" = @p1
                        )
                        """
                    ],
                    expectedParameters: expectedParams
                ),
                new SqlTestCase(
                    SqlDialectKind.SqlServer,
                    [
                        """
                        DELETE FROM [OrderLine]
                        WHERE [OrderLine].[OrderId] IN (
                            SELECT [dbo].[Orders].[Id]
                            FROM [dbo].[Orders]
                            WHERE [dbo].[Orders].[order_status] = @p0
                        )
                        """
                    ],
                    expectedParameters: expectedParams
                )
            ];
        }
    }
}