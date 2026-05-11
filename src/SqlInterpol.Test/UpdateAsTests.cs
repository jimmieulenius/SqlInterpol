using SqlInterpol.Config;
using SqlInterpol.Metadata;
using SqlInterpol.Test.Dialects;
using SqlInterpol.Test.Models;

namespace SqlInterpol.Test;

public class UpdateAsTests
{
    [SqlTable("Orders", Schema = "dbo")]
    public record OrderModel
    {
        public int Id { get; init; }

        [SqlColumn("order_status")]
        public string Status { get; init; } = "";
        
        public decimal Total { get; init; }
    }

    [Theory]
    [MemberData(nameof(UpdateSetWithAliasData))]
    public void UpdateSet_WithExplicitAlias_StripsPrefixInSetClause(SqlTestCase testCase)
    {
        // Arrange
        var db = testCase.CreateBuilder();
        var updateDto = new { Status = "Shipped", Total = 99.99m };
        
        // Act
        var result = db.Query<OrderModel>(o =>
            db.Append($$"""
            UPDATE {{o}} AS {{o.Alias("ord")}}
            SET {{Sql.UpdateSet(o, updateDto)}}
            WHERE {{o[x => x.Id]}} = 1
            """))
            .Build();

        // Assert
        Assert.Equal(testCase.ExpectedSql[0], result.Sql);
        
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