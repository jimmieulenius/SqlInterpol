using SqlInterpol.Configuration;
using SqlInterpol.Schema;
using SqlInterpol.Test.Dialects;
using SqlInterpol.Test.Models;

namespace SqlInterpol.Test;

public class GroupByTests
{
    [Theory]
    [MemberData(nameof(GroupByCombinerData))]
    public void GroupBy_EntityExpression(SqlTestCase testCase)
    {
        // Arrange
        var db = testCase.CreateBuilder();
        
        // Act - Uses zero-allocation POCO property routing
        testCase.Action(() => db.Entity<OrderModel>(out var o)
            .Append($$"""
            SELECT CategoryId, order_status, COUNT(*)
            FROM {{o}}
            GROUP BY {{o.CategoryId}}, {{o.Status}}
            """)
            .Build()
        );

        // Assert
        testCase.Assert();
    }

    [Theory]
    [MemberData(nameof(GroupByCombinerData))]
    public void GroupBy_WithEnumerableCombiner(SqlTestCase testCase)
    {
        // Arrange
        var db = testCase.CreateBuilder();

        // Simulate generating fragments dynamically from an API request using C# property names
        string[] apiRequestFields = ["CategoryId", "Status"];

        // Act - Testing dynamic API scenario using LINQ Select and the Column extension
        testCase.Action(() => db.Entity<OrderModel>(out var o)
            .Append($$"""
            SELECT CategoryId, order_status, COUNT(*)
            FROM {{o}}
            GROUP BY {{apiRequestFields.Select(f => o.Column(f))}}
            """)
            .Build()
        );

        // Assert
        testCase.Assert();
    }

    [Theory]
    [MemberData(nameof(GroupByWithSqlRawData))]
    public void GroupBy_WithSqlRaw(SqlTestCase testCase)
    {
        // Arrange
        var db = testCase.CreateBuilder();

        // Act
        testCase.Action(() => db.Entity<OrderModel>(out var o)
            .Append($$"""
            SELECT YEAR(created_at), COUNT(*)
            FROM {{o}}
            GROUP BY {{Sql.Raw("YEAR(created_at)")}}
            """)
            .Build()
        );

        // Assert
        testCase.Assert();
    }

    [Theory]
    [MemberData(nameof(GroupByMixingTypedAndRawData))]
    public void GroupBy_MixingTypedAndRaw(SqlTestCase testCase)
    {
        // Arrange
        var db = testCase.CreateBuilder();

        // Act
        testCase.Action(() => db.Entity<OrderModel>(out var o)
            .Append($$"""
            SELECT order_status, YEAR(created_at), COUNT(*)
            FROM {{o}}
            GROUP BY {{o.Status}}, {{Sql.Raw("YEAR(created_at)")}}
            """)
            .Build()
        );

        // Assert
        testCase.Assert();
    }

    public static TheoryData<SqlTestCase> GroupByCombinerData =>
    [
        new SqlTestCase(
            SqlDialectKind.CustomDb,
            [
                """
                SELECT CategoryId, order_status, COUNT(*)
                FROM <<dbo>>.<<Orders>>
                GROUP BY <<dbo>>.<<Orders>>.<<CategoryId>>, <<dbo>>.<<Orders>>.<<order_status>>
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.Firebird,
            [
                """
                SELECT CategoryId, order_status, COUNT(*)
                FROM "dbo"."Orders"
                GROUP BY "dbo"."Orders"."CategoryId", "dbo"."Orders"."order_status"
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.MySql,
            [
                """
                SELECT CategoryId, order_status, COUNT(*)
                FROM `dbo`.`Orders`
                GROUP BY `dbo`.`Orders`.`CategoryId`, `dbo`.`Orders`.`order_status`
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.Oracle,
            [
                """
                SELECT CategoryId, order_status, COUNT(*)
                FROM "dbo"."Orders"
                GROUP BY "dbo"."Orders"."CategoryId", "dbo"."Orders"."order_status"
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.PostgreSql,
            [
                """
                SELECT CategoryId, order_status, COUNT(*)
                FROM "dbo"."Orders"
                GROUP BY "dbo"."Orders"."CategoryId", "dbo"."Orders"."order_status"
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.SqLite,
            [
                """
                SELECT CategoryId, order_status, COUNT(*)
                FROM "dbo"."Orders"
                GROUP BY "dbo"."Orders"."CategoryId", "dbo"."Orders"."order_status"
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.SqlServer,
            [
                """
                SELECT CategoryId, order_status, COUNT(*)
                FROM [dbo].[Orders]
                GROUP BY [dbo].[Orders].[CategoryId], [dbo].[Orders].[order_status]
                """
            ]
        )
    ];

    public static TheoryData<SqlTestCase> GroupByWithSqlRawData =>
    [
        new SqlTestCase(
            SqlDialectKind.CustomDb,
            [
                """
                SELECT YEAR(created_at), COUNT(*)
                FROM <<dbo>>.<<Orders>>
                GROUP BY YEAR(created_at)
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.MySql,
            [
                """
                SELECT YEAR(created_at), COUNT(*)
                FROM `dbo`.`Orders`
                GROUP BY YEAR(created_at)
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.Oracle,
            [
                """
                SELECT YEAR(created_at), COUNT(*)
                FROM "dbo"."Orders"
                GROUP BY YEAR(created_at)
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.PostgreSql,
            [
                """
                SELECT YEAR(created_at), COUNT(*)
                FROM "dbo"."Orders"
                GROUP BY YEAR(created_at)
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.SqLite,
            [
                """
                SELECT YEAR(created_at), COUNT(*)
                FROM "dbo"."Orders"
                GROUP BY YEAR(created_at)
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.Firebird,
            [
                """
                SELECT YEAR(created_at), COUNT(*)
                FROM "dbo"."Orders"
                GROUP BY YEAR(created_at)
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.SqlServer,
            [
                """
                SELECT YEAR(created_at), COUNT(*)
                FROM [dbo].[Orders]
                GROUP BY YEAR(created_at)
                """
            ]
        )
    ];

    public static TheoryData<SqlTestCase> GroupByMixingTypedAndRawData =>
    [
        new SqlTestCase(
            SqlDialectKind.CustomDb,
            [
                """
                SELECT order_status, YEAR(created_at), COUNT(*)
                FROM <<dbo>>.<<Orders>>
                GROUP BY <<dbo>>.<<Orders>>.<<order_status>>, YEAR(created_at)
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.MySql,
            [
                """
                SELECT order_status, YEAR(created_at), COUNT(*)
                FROM `dbo`.`Orders`
                GROUP BY `dbo`.`Orders`.`order_status`, YEAR(created_at)
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.Oracle,
            [
                """
                SELECT order_status, YEAR(created_at), COUNT(*)
                FROM "dbo"."Orders"
                GROUP BY "dbo"."Orders"."order_status", YEAR(created_at)
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.PostgreSql,
            [
                """
                SELECT order_status, YEAR(created_at), COUNT(*)
                FROM "dbo"."Orders"
                GROUP BY "dbo"."Orders"."order_status", YEAR(created_at)
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.SqLite,
            [
                """
                SELECT order_status, YEAR(created_at), COUNT(*)
                FROM "dbo"."Orders"
                GROUP BY "dbo"."Orders"."order_status", YEAR(created_at)
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.Firebird,
            [
                """
                SELECT order_status, YEAR(created_at), COUNT(*)
                FROM "dbo"."Orders"
                GROUP BY "dbo"."Orders"."order_status", YEAR(created_at)
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.SqlServer,
            [
                """
                SELECT order_status, YEAR(created_at), COUNT(*)
                FROM [dbo].[Orders]
                GROUP BY [dbo].[Orders].[order_status], YEAR(created_at)
                """
            ]
        )
    ];
}