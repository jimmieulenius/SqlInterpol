using SqlInterpol.Config;
using SqlInterpol.Metadata;
using SqlInterpol.Test.Dialects;
using SqlInterpol.Test.Models;

namespace SqlInterpol.Test;

public class FormattingTests
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
    [MemberData(nameof(VerticalInsertData))]
    public void Insert_WithVerticalLayout_IndentsCorrectly(SqlTestCase testCase)
    {
        // Arrange
        var db = testCase.CreateBuilder();
        db.Context.Options.CollectionLayout = SqlCollectionLayout.Vertical;
        db.Context.Options.IndentSize = 4;
        var dto = new { Status = "New", Total = 10m };

        // Act
        var result = db.Query<OrderModel>(o => 
            db.Append($"{Sql.Insert(o, dto)}"))
            .Build();

        // Assert
        Assert.Equal(testCase.ExpectedSql[0], result.Sql);
    }

    public static TheoryData<SqlTestCase> VerticalInsertData =>
    [
        new SqlTestCase(
            SqlDialectKind.CustomDb,
            [
                $$"""
                INSERT INTO <<dbo>>.<<Orders>>{{Environment.NewLine
                }}({{Environment.NewLine
                }}    <<order_status>>,{{Environment.NewLine
                }}    <<Total>>{{Environment.NewLine
                }}){{Environment.NewLine
                }}VALUES{{Environment.NewLine
                }}({{Environment.NewLine
                }}    !!100,{{Environment.NewLine
                }}    !!101{{Environment.NewLine
                }})
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.MySql,
            [
                $$"""
                INSERT INTO `dbo`.`Orders`{{Environment.NewLine
                }}({{Environment.NewLine
                }}    `order_status`,{{Environment.NewLine
                }}    `Total`{{Environment.NewLine
                }}){{Environment.NewLine
                }}VALUES{{Environment.NewLine
                }}({{Environment.NewLine
                }}    @p0,{{Environment.NewLine
                }}    @p1{{Environment.NewLine
                }})
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.Oracle,
            [
                $$"""
                INSERT INTO "dbo"."Orders"{{Environment.NewLine
                }}({{Environment.NewLine
                }}    "order_status",{{Environment.NewLine
                }}    "Total"{{Environment.NewLine
                }}){{Environment.NewLine
                }}VALUES{{Environment.NewLine
                }}({{Environment.NewLine
                }}    :0,{{Environment.NewLine
                }}    :1{{Environment.NewLine
                }})
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.PostgreSql,
            [
                $$"""
                INSERT INTO "dbo"."Orders"{{Environment.NewLine
                }}({{Environment.NewLine
                }}    "order_status",{{Environment.NewLine
                }}    "Total"{{Environment.NewLine
                }}){{Environment.NewLine
                }}VALUES{{Environment.NewLine
                }}({{Environment.NewLine
                }}    $1,{{Environment.NewLine
                }}    $2{{Environment.NewLine
                }})
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.SqLite,
            [
                $$"""
                INSERT INTO "dbo"."Orders"{{Environment.NewLine
                }}({{Environment.NewLine
                }}    "order_status",{{Environment.NewLine
                }}    "Total"{{Environment.NewLine
                }}){{Environment.NewLine
                }}VALUES{{Environment.NewLine
                }}({{Environment.NewLine
                }}    ?0,{{Environment.NewLine
                }}    ?1{{Environment.NewLine
                }})
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.SqlServer,
            [
                $$"""
                INSERT INTO [dbo].[Orders]{{Environment.NewLine
                }}({{Environment.NewLine
                }}    [order_status],{{Environment.NewLine
                }}    [Total]{{Environment.NewLine
                }}){{Environment.NewLine
                }}VALUES{{Environment.NewLine
                }}({{Environment.NewLine
                }}    @p0,{{Environment.NewLine
                }}    @p1{{Environment.NewLine
                }})
                """
            ]
        )
    ];

    [Theory]
    [MemberData(nameof(HorizontalInsertData))]
    public void Insert_WithHorizontalLayout_RendersInline(SqlTestCase testCase)
    {
        // Arrange
        var db = testCase.CreateBuilder(); 
        var dto = new { Status = "New", Total = 10m };

        // Act
        var result = db.Query<OrderModel>(o => 
            db.Append($"{Sql.Insert(o, dto)}"))
            .Build();

        // Assert
        Assert.Equal(testCase.ExpectedSql[0], result.Sql);
    }

    public static TheoryData<SqlTestCase> HorizontalInsertData =>
    [
        new SqlTestCase(
            SqlDialectKind.CustomDb,
            [
                """
                INSERT INTO <<dbo>>.<<Orders>>
                (<<order_status>>, <<Total>>)
                VALUES (!!100, !!101)
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.MySql,
            [
                """
                INSERT INTO `dbo`.`Orders`
                (`order_status`, `Total`)
                VALUES (@p0, @p1)
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.Oracle,
            [
                """
                INSERT INTO "dbo"."Orders"
                ("order_status", "Total")
                VALUES (:0, :1)
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.PostgreSql,
            [
                """
                INSERT INTO "dbo"."Orders"
                ("order_status", "Total")
                VALUES ($1, $2)
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.SqLite,
            [
                """
                INSERT INTO "dbo"."Orders"
                ("order_status", "Total")
                VALUES (?0, ?1)
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.SqlServer,
            [
                """
                INSERT INTO [dbo].[Orders]
                ([order_status], [Total])
                VALUES (@p0, @p1)
                """
            ]
        )
    ];

    [Theory]
    [MemberData(nameof(VerticalUpdateData))]
    public void Update_WithVerticalLayout_IndentsCorrectly(SqlTestCase testCase)
    {
        // Arrange
        var db = testCase.CreateBuilder();
        db.Context.Options.CollectionLayout = SqlCollectionLayout.Vertical;
        db.Context.Options.IndentSize = 4;
        var dto = new { Status = "Processing", Total = 50.00m };

        // Act
        var result = db.Query<OrderModel>(o => 
            db.Append($"{Sql.Update(o, dto)}"))
            .Build();

        // Assert
        Assert.Equal(testCase.ExpectedSql[0], result.Sql);
    }

    public static TheoryData<SqlTestCase> VerticalUpdateData =>
    [
        new SqlTestCase(
            SqlDialectKind.CustomDb,
            [
                $$"""
                UPDATE <<dbo>>.<<Orders>>{{Environment.NewLine
                }}SET{{Environment.NewLine
                }}    <<order_status>> = !!100,{{Environment.NewLine
                }}    <<Total>> = !!101
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.MySql,
            [
                $$"""
                UPDATE `dbo`.`Orders`{{Environment.NewLine
                }}SET{{Environment.NewLine
                }}    `order_status` = @p0,{{Environment.NewLine
                }}    `Total` = @p1
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.Oracle,
            [
                $$"""
                UPDATE "dbo"."Orders"{{Environment.NewLine
                }}SET{{Environment.NewLine
                }}    "order_status" = :0,{{Environment.NewLine
                }}    "Total" = :1
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.PostgreSql,
            [
                $$"""
                UPDATE "dbo"."Orders"{{Environment.NewLine
                }}SET{{Environment.NewLine
                }}    "order_status" = $1,{{Environment.NewLine
                }}    "Total" = $2
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.SqLite,
            [
                $$"""
                UPDATE "dbo"."Orders"{{Environment.NewLine
                }}SET{{Environment.NewLine
                }}    "order_status" = ?0,{{Environment.NewLine
                }}    "Total" = ?1
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.SqlServer,
            [
                $$"""
                UPDATE [dbo].[Orders]{{Environment.NewLine
                }}SET{{Environment.NewLine
                }}    [order_status] = @p0,{{Environment.NewLine
                }}    [Total] = @p1
                """
            ]
        )
    ];

    [Theory]
    [MemberData(nameof(VerticalWhereInData))]
    public void WhereIn_WithVerticalLayout_IndentsCorrectly(SqlTestCase testCase)
    {
        // Arrange
        var db = testCase.CreateBuilder();
        db.Context.Options.CollectionLayout = SqlCollectionLayout.Vertical;
        db.Context.Options.IndentSize = 4;
        var ids = new[] { 1, 2, 3 };

        // Act
        var result = db.Query<OrderModel>(o => 
            db.Append($$"""
            SELECT *
            FROM {{o}}
            WHERE {{o[x => x.Id]}} IN (
                {{ids}}
            )
            """))   
            .Build();

        // Assert
        Assert.Equal(testCase.ExpectedSql[0], result.Sql);
    }

    public static TheoryData<SqlTestCase> VerticalWhereInData =>
    [
        new SqlTestCase(
            SqlDialectKind.CustomDb,
            [
                $$"""
                SELECT *{{Environment.NewLine
                }}FROM <<dbo>>.<<Orders>>{{Environment.NewLine
                }}WHERE <<dbo>>.<<Orders>>.<<Id>> IN ({{Environment.NewLine
                }}    !!100,{{Environment.NewLine
                }}    !!101,{{Environment.NewLine
                }}    !!102{{Environment.NewLine
                }})
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.MySql,
            [
                $$"""
                SELECT *{{Environment.NewLine
                }}FROM `dbo`.`Orders`{{Environment.NewLine
                }}WHERE `dbo`.`Orders`.`Id` IN ({{Environment.NewLine
                }}    @p0,{{Environment.NewLine
                }}    @p1,{{Environment.NewLine
                }}    @p2{{Environment.NewLine
                }})
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.Oracle,
            [
                $$"""
                SELECT *{{Environment.NewLine
                }}FROM "dbo"."Orders"{{Environment.NewLine
                }}WHERE "dbo"."Orders"."Id" IN ({{Environment.NewLine
                }}    :0,{{Environment.NewLine
                }}    :1,{{Environment.NewLine
                }}    :2{{Environment.NewLine
                }})
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.PostgreSql,
            [
                $$"""
                SELECT *{{Environment.NewLine
                }}FROM "dbo"."Orders"{{Environment.NewLine
                }}WHERE "dbo"."Orders"."Id" IN ({{Environment.NewLine
                }}    $1,{{Environment.NewLine
                }}    $2,{{Environment.NewLine
                }}    $3{{Environment.NewLine
                }})
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.SqLite,
            [
                $$"""
                SELECT *{{Environment.NewLine
                }}FROM "dbo"."Orders"{{Environment.NewLine
                }}WHERE "dbo"."Orders"."Id" IN ({{Environment.NewLine
                }}    ?0,{{Environment.NewLine
                }}    ?1,{{Environment.NewLine
                }}    ?2{{Environment.NewLine
                }})
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.SqlServer,
            [
                $$"""
                SELECT *{{Environment.NewLine
                }}FROM [dbo].[Orders]{{Environment.NewLine
                }}WHERE [dbo].[Orders].[Id] IN ({{Environment.NewLine
                }}    @p0,{{Environment.NewLine
                }}    @p1,{{Environment.NewLine
                }}    @p2{{Environment.NewLine
                }})
                """
            ]
        )
    ];
}