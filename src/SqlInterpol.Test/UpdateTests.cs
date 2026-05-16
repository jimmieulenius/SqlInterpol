using SqlInterpol.Config;
using SqlInterpol.Test.Dialects;
using SqlInterpol.Test.Models;

namespace SqlInterpol.Test;

public class UpdateTests
{
    [Theory]
    [MemberData(nameof(AllDialectsUpdateData))]
    public void Update_WithContextualDto(SqlTestCase testCase)
    {
        // Arrange
        var db = testCase.CreateBuilder();
        var updateDto = new { Status = "Shipped", Total = 99.99m };
        int orderId = 42;

        // Act - Using the new contextual parser!
        var result = db.Query<OrderModel>(o => db.Append($$"""
            UPDATE {{o}}
            SET {{updateDto}}
            WHERE {{o[x => x.Id]}} = {{orderId}}
            """)).Build();

        // Assert SQL
        testCase.AssertSql(result.Sql);

        // Assert Parameters
        Assert.Equal(3, result.Parameters.Count);
        Assert.Equal("Shipped", result.Parameters.ElementAt(0).Value);
        Assert.Equal(99.99m, result.Parameters.ElementAt(1).Value);
        Assert.Equal(42, result.Parameters.ElementAt(2).Value);
    }

    [Theory]
    [MemberData(nameof(UpdateExplicitData))]
    public void Update_PureManual(SqlTestCase testCase)
    {
        // Arrange
        var db = testCase.CreateBuilder();
        var status = "Shipped";
        var total = 99.99m;
        int orderId = 42;
        
        // Act - Pure raw SQL mapping
        var result = db.Query<OrderModel>(o => db.Append($$"""
            UPDATE {{o}}
            SET {{o[x => x.Status]}} = {{status}}, {{o[x => x.Total]}} = {{total}}
            WHERE {{o[x => x.Id]}} = {{orderId}}
            """)).Build();

        // Assert SQL
        testCase.AssertSql(result.Sql);
        
        // Assert Parameters
        Assert.Equal(3, result.Parameters.Count);
        Assert.Equal(status, result.Parameters.ElementAt(0).Value);
        Assert.Equal(total, result.Parameters.ElementAt(1).Value);
        Assert.Equal(orderId, result.Parameters.ElementAt(2).Value);
    }

    public static TheoryData<SqlTestCase> AllDialectsUpdateData =>
    [
        new SqlTestCase(
            SqlDialectKind.CustomDb,
            [
                """
                UPDATE <<dbo>>.<<Orders>>
                SET <<order_status>> = !!100, <<Total>> = !!101
                WHERE <<dbo>>.<<Orders>>.<<Id>> = !!102
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.MySql,
            [
                """
                UPDATE `dbo`.`Orders`
                SET `order_status` = @p0, `Total` = @p1
                WHERE `dbo`.`Orders`.`Id` = @p2
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.Oracle,
            [
                """
                UPDATE "dbo"."Orders"
                SET "order_status" = :0, "Total" = :1
                WHERE "dbo"."Orders"."Id" = :2
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.PostgreSql,
            [
                """
                UPDATE "dbo"."Orders"
                SET "order_status" = $1, "Total" = $2
                WHERE "dbo"."Orders"."Id" = $3
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.SqLite,
            [
                """
                UPDATE "dbo"."Orders"
                SET "order_status" = ?0, "Total" = ?1
                WHERE "dbo"."Orders"."Id" = ?2
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.SqlServer,
            [
                """
                UPDATE [dbo].[Orders]
                SET [order_status] = @p0, [Total] = @p1
                WHERE [dbo].[Orders].[Id] = @p2
                """
            ]
        )
    ];

    // Expected data for the Explicit/Manual tests (Notice how they render slightly differently 
    // because manual uses fully qualified aliases like [dbo].[Orders].[order_status] = ...)
    public static TheoryData<SqlTestCase> UpdateExplicitData =>
    [
        new SqlTestCase(
            SqlDialectKind.CustomDb,
            [
                """
                UPDATE <<dbo>>.<<Orders>>
                SET <<dbo>>.<<Orders>>.<<order_status>> = !!100, <<dbo>>.<<Orders>>.<<Total>> = !!101
                WHERE <<dbo>>.<<Orders>>.<<Id>> = !!102
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.MySql,
            [
                """
                UPDATE `dbo`.`Orders`
                SET `dbo`.`Orders`.`order_status` = @p0, `dbo`.`Orders`.`Total` = @p1
                WHERE `dbo`.`Orders`.`Id` = @p2
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.Oracle,
            [
                """
                UPDATE "dbo"."Orders"
                SET "dbo"."Orders"."order_status" = :0, "dbo"."Orders"."Total" = :1
                WHERE "dbo"."Orders"."Id" = :2
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.PostgreSql,
            [
                """
                UPDATE "dbo"."Orders"
                SET "dbo"."Orders"."order_status" = $1, "dbo"."Orders"."Total" = $2
                WHERE "dbo"."Orders"."Id" = $3
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.SqLite,
            [
                """
                UPDATE "dbo"."Orders"
                SET "dbo"."Orders"."order_status" = ?0, "dbo"."Orders"."Total" = ?1
                WHERE "dbo"."Orders"."Id" = ?2
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.SqlServer,
            [
                """
                UPDATE [dbo].[Orders]
                SET [dbo].[Orders].[order_status] = @p0, [dbo].[Orders].[Total] = @p1
                WHERE [dbo].[Orders].[Id] = @p2
                """
            ]
        )
    ];
}