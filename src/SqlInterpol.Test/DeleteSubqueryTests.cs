using SqlInterpol.Config;
using SqlInterpol.Test.Dialects;
using SqlInterpol.Test.Models;

namespace SqlInterpol.Test;

public class DeleteSubqueryTests
{
    [Theory]
    [MemberData(nameof(Delete_WithSubqueryData))]
    public void Delete_WithSubquery(SqlTestCase testCase)
    {
        // Arrange
        var db = testCase.CreateBuilder();
        var status = "Cancelled";

        // Act
        var result = db.Query<OrderLine, OrderModel>((l, o) =>
            db.Append($$"""
            DELETE FROM {{l}} 
            WHERE {{l[x => x.OrderId]}} IN (
                SELECT {{o[x => x.Id]}} 
                FROM {{o}} 
                WHERE {{o[x => x.Status]}} = {{status}}
            )
            """))
            .Build();

        // Assert
        testCase.AssertSql(result.Sql);
        Assert.Equal(status, result.Parameters.ElementAt(0).Value);
    }

    public static TheoryData<SqlTestCase> Delete_WithSubqueryData =>
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
            ]
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
            ]
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
            ]
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
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.SqLite,
            [
                """
                DELETE FROM "OrderLine" 
                WHERE "OrderLine"."OrderId" IN (
                    SELECT "dbo"."Orders"."Id" 
                    FROM "dbo"."Orders" 
                    WHERE "dbo"."Orders"."order_status" = ?0
                )
                """
            ]
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
            ]
        )
    ];
}