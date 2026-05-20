using SqlInterpol.Test.Dialects;
using SqlInterpol.Test.Models;

namespace SqlInterpol.Test;

public class OrderByTests
{
    [Theory]
    [MemberData(nameof(OrderByExpressionData))]
    public void OrderBy_EntityExpression(SqlTestCase testCase)
    {
        // Arrange
        var db = testCase.CreateBuilder();
        
        // Act
        var result = db.Query<OrderModel>(o =>
            db.Append($$"""
            SELECT *
            FROM {{o}}
            ORDER BY {{o.OrderBy(x => x.CreatedAt, SqlOrderDirection.Desc)}}
            """))
            .Build();

        // Assert
        testCase.AssertSql(result.Sql);
    }

    [Theory]
    [MemberData(nameof(OrderByCombinerData))]
    public void OrderBy_WithParamsCombiner(SqlTestCase testCase)
    {
        // Arrange
        var db = testCase.CreateBuilder();

        // Act - Tests the Sql.OrderBy(params) overload
        var result = db.Query<OrderModel>(o =>
            db.Append($$"""
            SELECT *
            FROM {{o}}
            ORDER BY {{o["Total"]}}, {{o[x => x.Id]}} {{SqlOrderDirection.Desc}}
            """))
            .Build();

        // Assert
        testCase.AssertSql(result.Sql);
    }

    [Theory]
    [MemberData(nameof(OrderByCombinerData))]
    public void OrderBy_WithEnumerableCombiner(SqlTestCase testCase)
    {
        // Arrange
        var db = testCase.CreateBuilder();

        // Act - Tests the dynamic API scenario using IEnumerable
        var result = db.Query<OrderModel>(o =>
        {
            // Simulate generating fragments dynamically from an API request
            IEnumerable<ISqlOrderFragment> sorts = 
            [
                o.OrderBy("Total"),
                o.OrderBy(x => x.Id, SqlOrderDirection.Desc)
            ];

            db.Append($$"""
                SELECT *
                FROM {{o}}
                ORDER BY {{sorts}}
                """);
        }).Build();

        // Assert
        testCase.AssertSql(result.Sql);
    }

    [Theory]
    [MemberData(nameof(OrderByRawData))]
    public void OrderBy_WithSqlRaw(SqlTestCase testCase)
    {
        // Arrange
        var db = testCase.CreateBuilder();

        // Act
        var result = db.Query<OrderModel>(o =>
            db.Append($$"""
            SELECT *
            FROM {{o}}
            ORDER BY {{Sql.Raw("Total DESC")}}
            """))
            .Build();

        // Assert
        testCase.AssertSql(result.Sql);
    }

    [Theory]
    [MemberData(nameof(OrderByMixedRawData))]
    public void OrderBy_MixingTypedAndRaw(SqlTestCase testCase)
    {
        // Arrange
        var db = testCase.CreateBuilder();

        // Act
        var result = db.Query<OrderModel>(o =>
            db.Append($$"""
            SELECT *
            FROM {{o}}
            ORDER BY {{o.OrderBy(x => x.CreatedAt, SqlOrderDirection.Asc)}}, {{Sql.Raw("(Total * 0.9) DESC")}}
            """))
            .Build();

        // Assert
        testCase.AssertSql(result.Sql);
    }

    [Theory]
    [MemberData(nameof(OrderByErrorData))]
    public void OrderBy_ValidationRules(SqlErrorTestCase testCase)
    {
        // Act
        var exception = Record.Exception(() => 
        {
            var db = testCase.CreateBuilder();
            if (testCase.ExpectedMessageSubstring.Contains("FakeColumn"))
            {
                db.AddEntity<Product>().OrderBy("FakeColumn", SqlOrderDirection.Asc);
            }
            else
            {
                db.AddEntity<OrderTestModel>().OrderBy(x => x.UnmappedProperty, SqlOrderDirection.Asc);
            }
        });

        // Assert
        testCase.AssertException(exception);
    }

    public static TheoryData<SqlTestCase> OrderByExpressionData =>
    [
        new SqlTestCase(
            SqlDialectKind.CustomDb,
            [
                """
                SELECT *
                FROM <<dbo>>.<<Orders>>
                ORDER BY <<dbo>>.<<Orders>>.<<created_at>> DESC
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.Firebird,
            [
                """
                SELECT *
                FROM "dbo"."Orders"
                ORDER BY "dbo"."Orders"."created_at" DESC
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.MySql,
            [
                """
                SELECT *
                FROM `dbo`.`Orders`
                ORDER BY `dbo`.`Orders`.`created_at` DESC
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.Oracle,
            [
                """
                SELECT *
                FROM "dbo"."Orders"
                ORDER BY "dbo"."Orders"."created_at" DESC
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.PostgreSql,
            [
                """
                SELECT *
                FROM "dbo"."Orders"
                ORDER BY "dbo"."Orders"."created_at" DESC
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.SqLite,
            [
                """
                SELECT *
                FROM "dbo"."Orders"
                ORDER BY "dbo"."Orders"."created_at" DESC
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.SqlServer,
            [
                """
                SELECT *
                FROM [dbo].[Orders]
                ORDER BY [dbo].[Orders].[created_at] DESC
                """
            ]
        )
    ];

    public static TheoryData<SqlTestCase> OrderByCombinerData =>
    [
        new SqlTestCase(
            SqlDialectKind.CustomDb,
            [
                """
                SELECT *
                FROM <<dbo>>.<<Orders>>
                ORDER BY <<dbo>>.<<Orders>>.<<Total>>, <<dbo>>.<<Orders>>.<<Id>> DESC
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.Firebird,
            [
                """
                SELECT *
                FROM "dbo"."Orders"
                ORDER BY "dbo"."Orders"."Total", "dbo"."Orders"."Id" DESC
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.MySql,
            [
                """
                SELECT *
                FROM `dbo`.`Orders`
                ORDER BY `dbo`.`Orders`.`Total`, `dbo`.`Orders`.`Id` DESC
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.Oracle,
            [
                """
                SELECT *
                FROM "dbo"."Orders"
                ORDER BY "dbo"."Orders"."Total", "dbo"."Orders"."Id" DESC
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.PostgreSql,
            [
                """
                SELECT *
                FROM "dbo"."Orders"
                ORDER BY "dbo"."Orders"."Total", "dbo"."Orders"."Id" DESC
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.SqLite,
            [
                """
                SELECT *
                FROM "dbo"."Orders"
                ORDER BY "dbo"."Orders"."Total", "dbo"."Orders"."Id" DESC
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.SqlServer,
            [
                """
                SELECT *
                FROM [dbo].[Orders]
                ORDER BY [dbo].[Orders].[Total], [dbo].[Orders].[Id] DESC
                """
            ]
        )
    ];

    public static TheoryData<SqlTestCase> OrderByRawData =>
    [
        new SqlTestCase(
            SqlDialectKind.CustomDb,
            [
                """
                SELECT *
                FROM <<dbo>>.<<Orders>>
                ORDER BY Total DESC
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.Firebird,
            [
                """
                SELECT *
                FROM "dbo"."Orders"
                ORDER BY Total DESC
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.MySql,
            [
                """
                SELECT *
                FROM `dbo`.`Orders`
                ORDER BY Total DESC
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.Oracle,
            [
                """
                SELECT *
                FROM "dbo"."Orders"
                ORDER BY Total DESC
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.PostgreSql,
            [
                """
                SELECT *
                FROM "dbo"."Orders"
                ORDER BY Total DESC
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.SqLite,
            [
                """
                SELECT *
                FROM "dbo"."Orders"
                ORDER BY Total DESC
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.SqlServer,
            [
                """
                SELECT *
                FROM [dbo].[Orders]
                ORDER BY Total DESC
                """
            ]
        )
    ];

    public static TheoryData<SqlTestCase> OrderByMixedRawData =>
    [
        new SqlTestCase(
            SqlDialectKind.CustomDb,
            [
                """
                SELECT *
                FROM <<dbo>>.<<Orders>>
                ORDER BY <<dbo>>.<<Orders>>.<<created_at>> ASC, (Total * 0.9) DESC
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.Firebird,
            [
                """
                SELECT *
                FROM "dbo"."Orders"
                ORDER BY "dbo"."Orders"."created_at" ASC, (Total * 0.9) DESC
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.MySql,
            [
                """
                SELECT *
                FROM `dbo`.`Orders`
                ORDER BY `dbo`.`Orders`.`created_at` ASC, (Total * 0.9) DESC
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.Oracle,
            [
                """
                SELECT *
                FROM "dbo"."Orders"
                ORDER BY "dbo"."Orders"."created_at" ASC, (Total * 0.9) DESC
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.PostgreSql,
            [
                """
                SELECT *
                FROM "dbo"."Orders"
                ORDER BY "dbo"."Orders"."created_at" ASC, (Total * 0.9) DESC
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.SqLite,
            [
                """
                SELECT *
                FROM "dbo"."Orders"
                ORDER BY "dbo"."Orders"."created_at" ASC, (Total * 0.9) DESC
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.SqlServer,
            [
                """
                SELECT *
                FROM [dbo].[Orders]
                ORDER BY [dbo].[Orders].[created_at] ASC, (Total * 0.9) DESC
                """
            ]
        )
    ];

    public static TheoryData<SqlErrorTestCase> OrderByErrorData =>
    [
        new SqlErrorTestCase(
            SqlDialectKind.CustomDb,
            typeof(ArgumentException),
            $"Property 'FakeColumn' not found on '{typeof(Product).Name}'."
        ),
        new SqlErrorTestCase(
            SqlDialectKind.CustomDb,
            typeof(ArgumentException),
            "Property 'UnmappedProperty' not mapped."
        )
    ];
}