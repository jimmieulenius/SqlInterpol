using System.Collections.Generic;
using SqlInterpol.Config;
using SqlInterpol.Test.Dialects;
using SqlInterpol.Test.Models;
using Xunit;

namespace SqlInterpol.Test;

public class GroupByTests
{
    [Theory]
    [MemberData(nameof(GroupByCombinerData))]
    public void GroupBy_EntityExpression(SqlTestCase testCase)
    {
        // Arrange
        var db = testCase.CreateBuilder();
        
        // Act
        var result = db.Query<OrderModel>(o =>
            db.Append($$"""
            SELECT CategoryId, order_status, COUNT(*)
            FROM {{o}}
            GROUP BY {{o[x => x.CategoryId]}}, {{o[x => x.Status]}}
            """))
            .Build();

        // Assert
        testCase.AssertSql(result.Sql);
    }

    [Theory]
    [MemberData(nameof(GroupByCombinerData))]
    public void GroupBy_WithEnumerableCombiner(SqlTestCase testCase)
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
                o["order_status"]
            ];

            db.Append($$"""
                SELECT CategoryId, order_status, COUNT(*)
                FROM {{o}}
                GROUP BY {{groups}}
                """);
        }).Build();

        // Assert
        testCase.AssertSql(result.Sql);
    }

    [Theory]
    [MemberData(nameof(GroupByWithSqlRawData))]
    public void GroupBy_WithSqlRaw(SqlTestCase testCase)
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
        testCase.AssertSql(result.Sql);
    }

    [Theory]
    [MemberData(nameof(GroupByMixingTypedAndRawData))]
    public void GroupBy_MixingTypedAndRaw(SqlTestCase testCase)
    {
        // Arrange
        var db = testCase.CreateBuilder();

        // Act
        var result = db.Query<OrderModel>(o =>
            db.Append($$"""
            SELECT order_status, YEAR(created_at), COUNT(*)
            FROM {{o}}
            GROUP BY {{o[x => x.Status]}}, {{Sql.Raw("YEAR(created_at)")}}
            """))
            .Build();

        // Assert
        testCase.AssertSql(result.Sql);
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