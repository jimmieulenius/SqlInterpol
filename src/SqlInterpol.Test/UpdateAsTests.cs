using SqlInterpol.Test.Dialects;
using SqlInterpol.Test.Models;

namespace SqlInterpol.Test;

public class UpdateAsTests
{
    [Theory]
    [MemberData(nameof(UpdateSetWithAliasData))]
    public void Update_WithExplicitAlias_StripsPrefixInSetClause(SqlTestCase testCase)
    {
        // Arrange
        var db = testCase.CreateBuilder();
        var updateDto = new { Status = "Shipped", Total = 99.99m };
        
        // Act
        var result = db
            .Entity<OrderModel>(alias: "ord")
            .Query(o =>
            db.Append($$"""
            UPDATE {{o}}
            SET {{updateDto}}
            WHERE {{o[x => x.Id]}} = 1
            """))
            .Build();

        // Assert
        testCase.AssertSql(result.Sql);
        
        Assert.Equal("Shipped", result.Parameters.ElementAt(0).Value);
        Assert.Equal(99.99m, result.Parameters.ElementAt(1).Value);
    }

    public static TheoryData<SqlTestCase> UpdateSetWithAliasData =>
    [
        new SqlTestCase(
            SqlDialectKind.CustomDb,
            [
                """
                UPDATE <<dbo>>.<<Orders>> AS <<ord>>
                SET <<order_status>> = !!100, <<Total>> = !!101
                WHERE <<ord>>.<<Id>> = 1
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.MySql,
            [
                """
                UPDATE `dbo`.`Orders` AS `ord`
                SET `order_status` = @p0, `Total` = @p1
                WHERE `ord`.`Id` = 1
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.Oracle,
            [
                """
                UPDATE "dbo"."Orders" AS "ord"
                SET "order_status" = :0, "Total" = :1
                WHERE "ord"."Id" = 1
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.PostgreSql,
            [
                """
                UPDATE "dbo"."Orders" AS "ord"
                SET "order_status" = $1, "Total" = $2
                WHERE "ord"."Id" = 1
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.SqLite,
            [
                """
                UPDATE "dbo"."Orders" AS "ord"
                SET "order_status" = ?0, "Total" = ?1
                WHERE "ord"."Id" = 1
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.SqlServer,
            [
                """
                UPDATE [dbo].[Orders] AS [ord]
                SET [order_status] = @p0, [Total] = @p1
                WHERE [ord].[Id] = 1
                """
            ]
        )
    ];
}