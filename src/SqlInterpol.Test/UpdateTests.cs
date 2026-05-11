using SqlInterpol.Config;
using SqlInterpol.Metadata;
using SqlInterpol.Test.Dialects;
using SqlInterpol.Test.Models;

namespace SqlInterpol.Test;

public class UpdateTests
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
    [MemberData(nameof(UpdateData))]
    public void Update_WithExplicitSets_RendersCorrectly(SqlTestCase testCase)
    {
        // Arrange
        var db = testCase.CreateBuilder();
        
        // Act
        var result = db.Query<OrderModel>(o =>
            db.Append($$"""
            {{Sql.Update(o, 
                Sql.Set(o[x => x.Status], "Shipped"),
                Sql.Set(o[x => x.Total], 99.99m)
            )}}
            WHERE {{o[x => x.Id]}} = 1
            """))
            .Build();

        // Assert
        Assert.Equal(testCase.ExpectedSql[0], result.Sql);
        
        Assert.Equal("Shipped", result.Parameters.ElementAt(0).Value);
        Assert.Equal(99.99m, result.Parameters.ElementAt(1).Value);
    }

    [Theory]
    [MemberData(nameof(UpdateData))]
    public void Update_WithDto_RendersCorrectly(SqlTestCase testCase)
    {
        // Arrange
        var db = testCase.CreateBuilder();
        var updateDto = new { Status = "Shipped", Total = 99.99m };
        
        // Act
        var result = db.Query<OrderModel>(o =>
            db.Append($$"""
            {{Sql.Update(o, updateDto)}}
            WHERE {{o[x => x.Id]}} = 1
            """))
            .Build();

        // Assert
        Assert.Equal(testCase.ExpectedSql[0], result.Sql);
        
        Assert.Equal("Shipped", result.Parameters.ElementAt(0).Value);
        Assert.Equal(99.99m, result.Parameters.ElementAt(1).Value);
    }

    public static TheoryData<SqlTestCase> UpdateData =>
    [
        new SqlTestCase(
            SqlDialectKind.CustomDb,
            [
                """
                UPDATE <<dbo>>.<<Orders>>
                SET <<order_status>> = !!100, <<Total>> = !!101
                WHERE <<dbo>>.<<Orders>>.<<Id>> = 1
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.MySql,
            [
                """
                UPDATE `dbo`.`Orders`
                SET `order_status` = @p0, `Total` = @p1
                WHERE `dbo`.`Orders`.`Id` = 1
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.Oracle,
            [
                """
                UPDATE "dbo"."Orders"
                SET "order_status" = :0, "Total" = :1
                WHERE "dbo"."Orders"."Id" = 1
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.PostgreSql,
            [
                """
                UPDATE "dbo"."Orders"
                SET "order_status" = $1, "Total" = $2
                WHERE "dbo"."Orders"."Id" = 1
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.SqLite,
            [
                """
                UPDATE "dbo"."Orders"
                SET "order_status" = ?0, "Total" = ?1
                WHERE "dbo"."Orders"."Id" = 1
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.SqlServer,
            [
                """
                UPDATE [dbo].[Orders]
                SET [order_status] = @p0, [Total] = @p1
                WHERE [dbo].[Orders].[Id] = 1
                """
            ]
        )
    ];

    [Theory]
    [MemberData(nameof(UpdateSetData))]
    public void UpdateSet_WithExplicitSets_RendersCorrectly(SqlTestCase testCase)
    {
        // Arrange
        var db = testCase.CreateBuilder();
        
        // Act
        var result = db.Query<OrderModel>(o =>
            db.Append($$"""
            UPDATE {{o.Declaration}}
            SET {{Sql.UpdateSet(
                Sql.Set(o[x => x.Status], "Shipped"),
                Sql.Set(o[x => x.Total], 99.99m)
            )}}
            WHERE {{o[x => x.Id]}} = 1
            """))
            .Build();

        // Assert
        Assert.Equal(testCase.ExpectedSql[0], result.Sql);
    }

    [Theory]
    [MemberData(nameof(UpdateSetData))]
    public void UpdateSet_WithDto_RendersCorrectly(SqlTestCase testCase)
    {
        // Arrange
        var db = testCase.CreateBuilder();
        var updateDto = new { Status = "Shipped", Total = 99.99m };
        
        // Act
        var result = db.Query<OrderModel>(o =>
            db.Append($$"""
            UPDATE {{o.Declaration}}
            SET {{Sql.UpdateSet(o, updateDto)}}
            WHERE {{o[x => x.Id]}} = 1
            """))
            .Build();

        // Assert
        Assert.Equal(testCase.ExpectedSql[0], result.Sql);
    }

    public static TheoryData<SqlTestCase> UpdateSetData =>
    [
        new SqlTestCase(
            SqlDialectKind.CustomDb,
            [
                """
                UPDATE <<dbo>>.<<Orders>>
                SET <<order_status>> = !!100, <<Total>> = !!101
                WHERE <<dbo>>.<<Orders>>.<<Id>> = 1
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.MySql,
            [
                """
                UPDATE `dbo`.`Orders`
                SET `order_status` = @p0, `Total` = @p1
                WHERE `dbo`.`Orders`.`Id` = 1
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.Oracle,
            [
                """
                UPDATE "dbo"."Orders"
                SET "order_status" = :0, "Total" = :1
                WHERE "dbo"."Orders"."Id" = 1
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.PostgreSql,
            [
                """
                UPDATE "dbo"."Orders"
                SET "order_status" = $1, "Total" = $2
                WHERE "dbo"."Orders"."Id" = 1
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.SqLite,
            [
                """
                UPDATE "dbo"."Orders"
                SET "order_status" = ?0, "Total" = ?1
                WHERE "dbo"."Orders"."Id" = 1
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.SqlServer,
            [
                """
                UPDATE [dbo].[Orders]
                SET [order_status] = @p0, [Total] = @p1
                WHERE [dbo].[Orders].[Id] = 1
                """
            ]
        )
    ];

    [Theory]
    [MemberData(nameof(UpdateWithWhereParamData))]
    public void Update_WithParametersInSetAndWhere_MaintainsSequentialIndices(SqlTestCase testCase)
    {
        // Arrange
        var db = testCase.CreateBuilder();
        var status = "Archived";
        var targetId = 500;
        
        // Act
        // This tests the mix of Fragment-driven parameters (Sql.Set)
        // and Parser-driven parameters ({targetId})
        var result = db.Query<OrderModel>(o =>
            db.Append($$"""
            {{Sql.Update(o, Sql.Set(o[x => x.Status], status))}}
            WHERE {{o[x => x.Id]}} = {{targetId}}
            """))
            .Build();

        // Assert
        Assert.Equal(testCase.ExpectedSql[0], result.Sql);
        
        // Verify both parameters were captured in order
        // @p0 comes from Sql.Set rendering first
        // @p1 comes from the WHERE clause parsed after
        Assert.Equal(status, result.Parameters.ElementAt(0).Value);
        Assert.Equal(targetId, result.Parameters.ElementAt(1).Value);
    }

    public static TheoryData<SqlTestCase> UpdateWithWhereParamData =>
    [
        new SqlTestCase(
            SqlDialectKind.CustomDb,
            [
                """
                UPDATE <<dbo>>.<<Orders>>
                SET <<order_status>> = !!100
                WHERE <<dbo>>.<<Orders>>.<<Id>> = !!101
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.MySql,
            [
                """
                UPDATE `dbo`.`Orders`
                SET `order_status` = @p0
                WHERE `dbo`.`Orders`.`Id` = @p1
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.PostgreSql,
            [
                """
                UPDATE "dbo"."Orders"
                SET "order_status" = $1
                WHERE "dbo"."Orders"."Id" = $2
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.SqLite,
            [
                """
                UPDATE "dbo"."Orders"
                SET "order_status" = ?0
                WHERE "dbo"."Orders"."Id" = ?1
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.SqlServer,
            [
                """
                UPDATE [dbo].[Orders]
                SET [order_status] = @p0
                WHERE [dbo].[Orders].[Id] = @p1
                """
            ]
        )
    ];
}