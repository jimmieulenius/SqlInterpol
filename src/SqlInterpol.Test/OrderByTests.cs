using SqlInterpol.Config;
using SqlInterpol.Metadata;
using SqlInterpol.Test.Dialects;
using SqlInterpol.Test.Models;

namespace SqlInterpol.Test;

public class OrderByTests
{
    [SqlTable("Orders")]
    public record OrderModel
    {
        public int Id { get; init; }

        public decimal Total { get; init; }

        [SqlColumn("created_at")]
        public DateTime CreatedAt { get; init; }
    }

    [Theory]
    [MemberData(nameof(OrderByExpressionData))]
    public void OrderBy_EntityExpression_ResolvesMetadata(SqlTestCase testCase)
    {
        // Arrange
        var db = testCase.CreateBuilder();
        
        // Act
        var result = db.Query<OrderModel>(o =>
            db.Append($$"""
            SELECT *
            FROM {{o}}
            ORDER BY
                {{o.OrderBy(x => x.CreatedAt, SqlOrderDirection.Desc)}}
            """))
            .Build();

        // Assert
        Assert.Equal(testCase.ExpectedSql[0], result.Sql);
    }

    public static TheoryData<SqlTestCase> OrderByExpressionData =>
    [
        new SqlTestCase(
            SqlDialectKind.CustomDb,
            [
                """
                SELECT *
                FROM <<Orders>>
                ORDER BY
                    <<Orders>>.<<created_at>> DESC
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.MySql,
            [
                """
                SELECT *
                FROM `Orders`
                ORDER BY
                    `Orders`.`created_at` DESC
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.Oracle,
            [
                """
                SELECT *
                FROM "Orders"
                ORDER BY
                    "Orders"."created_at" DESC
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.PostgreSql,
            [
                """
                SELECT *
                FROM "Orders"
                ORDER BY
                    "Orders"."created_at" DESC
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.SqLite,
            [
                """
                SELECT *
                FROM "Orders"
                ORDER BY
                    "Orders"."created_at" DESC
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.SqlServer,
            [
                """
                SELECT *
                FROM [Orders]
                ORDER BY
                    [Orders].[created_at] DESC
                """
            ]
        )
    ];

    [Theory]
    [MemberData(nameof(OrderByChainedData))]
    public void OrderBy_ThenBy_ChainsCorrectly(SqlTestCase testCase)
    {
        // Arrange
        var db = testCase.CreateBuilder();

        // Act
        var result = db.Query<OrderModel>(o =>
            db.Append($$"""
            SELECT *
            FROM {{o}}
            ORDER BY {{o.OrderBy("Total", SqlOrderDirection.Desc)
                .ThenBy(o[x => x.Id], SqlOrderDirection.Asc)}}
            """))
            .Build();

        // Assert
        Assert.Equal(testCase.ExpectedSql[0], result.Sql);
    }

    public static TheoryData<SqlTestCase> OrderByChainedData =>
    [
        new SqlTestCase(
            SqlDialectKind.CustomDb,
            [
                """
                SELECT *
                FROM <<Orders>>
                ORDER BY <<Orders>>.<<Total>> DESC, <<Orders>>.<<Id>> ASC
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.MySql,
            [
                """
                SELECT *
                FROM `Orders`
                ORDER BY `Orders`.`Total` DESC, `Orders`.`Id` ASC
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.Oracle,
            [
                """
                SELECT *
                FROM "Orders"
                ORDER BY "Orders"."Total" DESC, "Orders"."Id" ASC
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.PostgreSql,
            [
                """
                SELECT *
                FROM "Orders"
                ORDER BY "Orders"."Total" DESC, "Orders"."Id" ASC
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.SqLite,
            [
                """
                SELECT *
                FROM "Orders"
                ORDER BY "Orders"."Total" DESC, "Orders"."Id" ASC
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.SqlServer,
            [
                """
                SELECT *
                FROM [Orders]
                ORDER BY [Orders].[Total] DESC, [Orders].[Id] ASC
                """
            ]
        )
    ];

    [Theory]
    [MemberData(nameof(OrderByRawData))]
    public void OrderBy_WithSqlRaw_RendersCorrectly(SqlTestCase testCase)
    {
        // Arrange
        var db = testCase.CreateBuilder();

        // Act
        var result = db.Query<OrderModel>(o =>
            db.Append($$"""
            SELECT *
            FROM {{o}}
            ORDER BY
                {{Sql.Raw("TotalValue DESC")}}
            """))
            .Build();

        // Assert
        Assert.Equal(testCase.ExpectedSql[0], result.Sql);
    }

    public static TheoryData<SqlTestCase> OrderByRawData =>
    [
        new SqlTestCase(
            SqlDialectKind.CustomDb,
            [
                """
                SELECT *
                FROM <<Orders>>
                ORDER BY
                    TotalValue DESC
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.MySql,
            [
                """
                SELECT *
                FROM `Orders`
                ORDER BY
                    TotalValue DESC
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.Oracle,
            [
                """
                SELECT *
                FROM "Orders"
                ORDER BY
                    TotalValue DESC
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.PostgreSql,
            [
                """
                SELECT *
                FROM "Orders"
                ORDER BY
                    TotalValue DESC
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.SqLite,
            [
                """
                SELECT *
                FROM "Orders"
                ORDER BY
                    TotalValue DESC
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.SqlServer,
            [
                """
                SELECT *
                FROM [Orders]
                ORDER BY
                    TotalValue DESC
                """
            ]
        )
    ];

    [Theory]
    [MemberData(nameof(OrderByMixedRawData))]
    public void OrderBy_MixingTypedAndRaw_RendersCorrectly(SqlTestCase testCase)
    {
        // Arrange
        var db = testCase.CreateBuilder();

        // Act - Proves we can mix typed chains with raw SQL via standard comma placement
        var result = db.Query<OrderModel>(o =>
            db.Append($$"""
            SELECT *
            FROM {{o}}
            ORDER BY
                {{o.OrderBy(x => x.CreatedAt, SqlOrderDirection.Asc)}},
                {{Sql.Raw("(Total * 0.9) DESC")}}
            """))
            .Build();

        // Assert
        Assert.Equal(testCase.ExpectedSql[0], result.Sql);
    }

    public static TheoryData<SqlTestCase> OrderByMixedRawData =>
    [
        new SqlTestCase(
            SqlDialectKind.CustomDb,
            [
                """
                SELECT *
                FROM <<Orders>>
                ORDER BY
                    <<Orders>>.<<created_at>> ASC,
                    (Total * 0.9) DESC
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.MySql,
            [
                """
                SELECT *
                FROM `Orders`
                ORDER BY
                    `Orders`.`created_at` ASC,
                    (Total * 0.9) DESC
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.Oracle,
            [
                """
                SELECT *
                FROM "Orders"
                ORDER BY
                    "Orders"."created_at" ASC,
                    (Total * 0.9) DESC
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.PostgreSql,
            [
                """
                SELECT *
                FROM "Orders"
                ORDER BY
                    "Orders"."created_at" ASC,
                    (Total * 0.9) DESC
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.SqLite,
            [
                """
                SELECT *
                FROM "Orders"
                ORDER BY
                    "Orders"."created_at" ASC,
                    (Total * 0.9) DESC
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.SqlServer,
            [
                """
                SELECT *
                FROM [Orders]
                ORDER BY
                    [Orders].[created_at] ASC,
                    (Total * 0.9) DESC
                """
            ]
        )
    ];
}