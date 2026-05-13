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
    [MemberData(nameof(AllDialectsUpdateData))]
    public void Update_WithContextualDto_RendersCorrectly(SqlTestCase testCase)
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
        var expectedSql = testCase.ExpectedSql[0].Replace("\r\n", "\n").Trim();
        var actualSql = result.Sql.Replace("\r\n", "\n").Trim();
        
        Assert.Equal(expectedSql, actualSql);

        // Assert Parameters
        Assert.Equal(3, result.Parameters.Count);
        Assert.Equal("Shipped", result.Parameters.ElementAt(0).Value);
        Assert.Equal(99.99m, result.Parameters.ElementAt(1).Value);
        Assert.Equal(42, result.Parameters.ElementAt(2).Value);
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
    [MemberData(nameof(UpdateExplicitData))]
    public void Update_PureManual_RendersCorrectly(SqlTestCase testCase)
    {
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

        var expectedSql = testCase.ExpectedSql[0].Replace("\r\n", "\n").Trim();
        var actualSql = result.Sql.Replace("\r\n", "\n").Trim();
        
        Assert.Equal(expectedSql, actualSql);
    }

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