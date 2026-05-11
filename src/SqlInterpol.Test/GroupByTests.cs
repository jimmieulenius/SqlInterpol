using SqlInterpol.Config;
using SqlInterpol.Metadata;
using SqlInterpol.Test.Dialects;
using SqlInterpol.Test.Models;

namespace SqlInterpol.Test;

public class GroupByTests
{
    [SqlTable("Orders")]
    public record OrderModel
    {
        public int Id { get; init; }

        public int CategoryId { get; init; }

        public string Status { get; init; } = "";

        [SqlColumn("created_at")]
        public DateTime CreatedAt { get; init; }
    }

    [Theory]
    [MemberData(nameof(GroupByCombinerData))]
    public void GroupBy_EntityExpression_ResolvesMetadata(SqlTestCase testCase)
    {
        // Arrange
        var db = testCase.CreateBuilder();
        
        // Act - Testing strongly-typed grouping using the params combiner
        var result = db.Query<OrderModel>(o =>
            db.Append($$"""
            SELECT CategoryId, Status, COUNT(*)
            FROM {{o}}
            GROUP BY {{Sql.GroupBy(o[x => x.CategoryId], o[x => x.Status])}}
            """))
            .Build();

        // Assert
        Assert.Equal(testCase.ExpectedSql[0], result.Sql);
    }

    [Theory]
    [MemberData(nameof(GroupByCombinerData))]
    public void GroupBy_WithEnumerableCombiner_RendersCorrectly(SqlTestCase testCase)
    {
        // Arrange
        var db = testCase.CreateBuilder();

        // Act - Testing dynamic API scenario using the string indexer and IEnumerable combiner
        var result = db.Query<OrderModel>(o =>
        {
            // Simulate generating fragments dynamically from an API request
            IEnumerable<ISqlFragment> groups = 
            [
                o["CategoryId"],
                o["Status"]
            ];

            db.Append($$"""
                SELECT CategoryId, Status, COUNT(*)
                FROM {{o}}
                GROUP BY {{Sql.GroupBy(groups)}}
                """);
        }).Build();

        // Assert
        Assert.Equal(testCase.ExpectedSql[0], result.Sql);
    }

    public static TheoryData<SqlTestCase> GroupByCombinerData =>
    [
        new SqlTestCase(
            SqlDialectKind.CustomDb,
            [
                """
                SELECT CategoryId, Status, COUNT(*)
                FROM <<Orders>>
                GROUP BY <<Orders>>.<<CategoryId>>, <<Orders>>.<<Status>>
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.MySql,
            [
                """
                SELECT CategoryId, Status, COUNT(*)
                FROM `Orders`
                GROUP BY `Orders`.`CategoryId`, `Orders`.`Status`
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.Oracle,
            [
                """
                SELECT CategoryId, Status, COUNT(*)
                FROM "Orders"
                GROUP BY "Orders"."CategoryId", "Orders"."Status"
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.PostgreSql,
            [
                """
                SELECT CategoryId, Status, COUNT(*)
                FROM "Orders"
                GROUP BY "Orders"."CategoryId", "Orders"."Status"
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.SqLite,
            [
                """
                SELECT CategoryId, Status, COUNT(*)
                FROM "Orders"
                GROUP BY "Orders"."CategoryId", "Orders"."Status"
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.SqlServer,
            [
                """
                SELECT CategoryId, Status, COUNT(*)
                FROM [Orders]
                GROUP BY [Orders].[CategoryId], [Orders].[Status]
                """
            ]
        )
    ];

    [Theory]
    [MemberData(nameof(GroupByRawData))]
    public void GroupBy_WithSqlRaw_RendersCorrectly(SqlTestCase testCase)
    {
        // Arrange
        var db = testCase.CreateBuilder();

        // Act
        var result = db.Query<OrderModel>(o =>
            db.Append($$"""
            SELECT YEAR(created_at), COUNT(*)
            FROM {{o}}
            GROUP BY {{Sql.Raw("YEAR(created_at)")}}
            """))
            .Build();

        // Assert
        Assert.Equal(testCase.ExpectedSql[0], result.Sql);
    }

    public static TheoryData<SqlTestCase> GroupByRawData =>
    [
        new SqlTestCase(
            SqlDialectKind.CustomDb,
            [
                """
                SELECT YEAR(created_at), COUNT(*)
                FROM <<Orders>>
                GROUP BY YEAR(created_at)
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.MySql,
            [
                """
                SELECT YEAR(created_at), COUNT(*)
                FROM `Orders`
                GROUP BY YEAR(created_at)
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.Oracle,
            [
                """
                SELECT YEAR(created_at), COUNT(*)
                FROM "Orders"
                GROUP BY YEAR(created_at)
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.PostgreSql,
            [
                """
                SELECT YEAR(created_at), COUNT(*)
                FROM "Orders"
                GROUP BY YEAR(created_at)
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.SqLite,
            [
                """
                SELECT YEAR(created_at), COUNT(*)
                FROM "Orders"
                GROUP BY YEAR(created_at)
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.SqlServer,
            [
                """
                SELECT YEAR(created_at), COUNT(*)
                FROM [Orders]
                GROUP BY YEAR(created_at)
                """
            ]
        )
    ];

    [Theory]
    [MemberData(nameof(GroupByMixedRawData))]
    public void GroupBy_MixingTypedAndRaw_RendersCorrectly(SqlTestCase testCase)
    {
        // Arrange
        var db = testCase.CreateBuilder();

        // Act
        var result = db.Query<OrderModel>(o =>
            db.Append($$"""
            SELECT Status, YEAR(created_at), COUNT(*)
            FROM {{o}}
            GROUP BY {{Sql.GroupBy(o[x => x.Status], Sql.Raw("YEAR(created_at)"))}}
            """))
            .Build();

        // Assert
        Assert.Equal(testCase.ExpectedSql[0], result.Sql);
    }

    public static TheoryData<SqlTestCase> GroupByMixedRawData =>
    [
        new SqlTestCase(
            SqlDialectKind.CustomDb,
            [
                """
                SELECT Status, YEAR(created_at), COUNT(*)
                FROM <<Orders>>
                GROUP BY <<Orders>>.<<Status>>, YEAR(created_at)
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.MySql,
            [
                """
                SELECT Status, YEAR(created_at), COUNT(*)
                FROM `Orders`
                GROUP BY `Orders`.`Status`, YEAR(created_at)
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.Oracle,
            [
                """
                SELECT Status, YEAR(created_at), COUNT(*)
                FROM "Orders"
                GROUP BY "Orders"."Status", YEAR(created_at)
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.PostgreSql,
            [
                """
                SELECT Status, YEAR(created_at), COUNT(*)
                FROM "Orders"
                GROUP BY "Orders"."Status", YEAR(created_at)
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.SqLite,
            [
                """
                SELECT Status, YEAR(created_at), COUNT(*)
                FROM "Orders"
                GROUP BY "Orders"."Status", YEAR(created_at)
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.SqlServer,
            [
                """
                SELECT Status, YEAR(created_at), COUNT(*)
                FROM [Orders]
                GROUP BY [Orders].[Status], YEAR(created_at)
                """
            ]
        )
    ];
}