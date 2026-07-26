using SqlInterpol.Configuration;
using SqlInterpol.Schema;
using SqlInterpol.Segments;
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
        testCase.Action(() => 
        {
            #pragma warning disable SQLIG10
            db.Entity<OrderModel>(out var o);
            return db.Append($$"""
            SELECT *
            FROM {{o}}
            ORDER BY {{o.OrderBy(x => x.CreatedAt, SqlOrderDirection.Desc)}}
            """).Build();
            #pragma warning restore SQLIG10
        });

        // Assert
        testCase.Assert();
    }

    [Theory]
    [MemberData(nameof(OrderByCombinerData))]
    public void OrderBy_WithParamsCombiner(SqlTestCase testCase)
    {
        // Arrange
        var db = testCase.CreateBuilder();

        // Act
        testCase.Action(() => 
        {
            db.Entity<OrderModel>(out var o);
            return db.Append($$"""
            SELECT *
            FROM {{o}}
            ORDER BY {{o.Total}}, {{o.Id}} DESC
            """).Build();
        });

        // Assert
        testCase.Assert();
        db.AssertAotIntercepted();
    }

    [Theory]
    [MemberData(nameof(OrderByEnumerableData))] // Bound to the correct explicit ASC dataset
    public void OrderBy_WithEnumerableCombiner(SqlTestCase testCase)
    {
        // Arrange
        var db = testCase.CreateBuilder();

        // Act - Simulates mapping an incoming string array directly from a Web API endpoint
        testCase.Action(() =>
        {
            db.Entity<OrderModel>(out var o);

            string[] apiSortFields = ["Total", "Id DESC"];

            var sorts = apiSortFields.Select(s =>
            {
                var parts = s.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                var propertyName = parts[0];
                var direction = parts.Length > 1 && parts[1].Equals("DESC", StringComparison.OrdinalIgnoreCase)
                    ? SqlOrderDirection.Desc
                    : SqlOrderDirection.Asc;

                return o.OrderBy(propertyName, direction);
            });

#pragma warning disable SQLIG10 // IEnumerable dynamically evaluates SQL fragments via JIT
            return db.Append($$"""
            SELECT *
            FROM {{o}}
            ORDER BY {{sorts}}
            """).Build();
#pragma warning restore SQLIG10
        });

        // Assert
        testCase.Assert();
        // Omitted db.AssertAotIntercepted() because IEnumerable forces JIT fallback
    }

    [Theory]
    [MemberData(nameof(OrderByRawData))]
    public void OrderBy_WithSqlRaw(SqlTestCase testCase)
    {
        // Arrange
        var db = testCase.CreateBuilder();

        // Act
        testCase.Action(() => 
        {
            db.Entity<OrderModel>(out var o);

#pragma warning disable SQLIG10 // Sql.Raw dynamically evaluates SQL fragments via JIT
            return db.Append($$"""
            SELECT *
            FROM {{o}}
            ORDER BY {{Sql.Raw("Total DESC")}}
            """).Build();
#pragma warning restore SQLIG10
        });

        // Assert
        testCase.Assert();
        // Omitted db.AssertAotIntercepted() because Sql.Raw forces JIT execution
    }

    [Theory]
    [MemberData(nameof(OrderByMixedRawData))]
    public void OrderBy_MixingTypedAndRaw(SqlTestCase testCase)
    {
        // Arrange
        var db = testCase.CreateBuilder();

        // Act
        testCase.Action(() => 
        {
            db.Entity<OrderModel>(out var o);

#pragma warning disable SQLIG10 // Sql.Raw dynamically evaluates SQL fragments via JIT
            return db.Append($$"""
            SELECT *
            FROM {{o}}
            ORDER BY {{o.OrderBy(x => x.CreatedAt, SqlOrderDirection.Asc)}}, {{Sql.Raw("(Total * 0.9) DESC")}}
            """).Build();
#pragma warning restore SQLIG10
        });

        // Assert
        testCase.Assert();
        // Omitted db.AssertAotIntercepted() because Sql.Raw forces JIT execution
    }

    [Theory]
    [MemberData(nameof(OrderByErrorData))]
    public void OrderBy_ValidationRules(SqlTestCase testCase)
    {
        // Act & Assert
        testCase.Action(() => 
        {
            #pragma warning disable SQLIG10
            var db = testCase.CreateBuilder();

            // By providing a valid "SELECT * FROM {entity}" baseline, the engine registers 
            // the table in the query scope. This allows the pipeline to advance to the 
            // column validation phase where it cleanly throws our expected ArgumentException!
            if (testCase.ExpectedExceptionMessage?.Contains("FakeColumn") == true)
            {
                db.Entity<Product>(out var p);
                return db.Append($"SELECT * FROM {p} ORDER BY {p.OrderBy("FakeColumn", SqlOrderDirection.Asc)}").Build();
            }
            else
            {
                db.Entity<OrderTestModel>(out var o);
                return db.Append($"SELECT * FROM {o} ORDER BY {o.OrderBy(x => x.UnmappedProperty, SqlOrderDirection.Asc)}").Build();
            }
            #pragma warning restore SQLIG10
        });

        testCase.Assert();
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

    public static TheoryData<SqlTestCase> OrderByEnumerableData =>
    [
        new SqlTestCase(
            SqlDialectKind.CustomDb,
            [
                """
                SELECT *
                FROM <<dbo>>.<<Orders>>
                ORDER BY <<dbo>>.<<Orders>>.<<Total>> ASC, <<dbo>>.<<Orders>>.<<Id>> DESC
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.Firebird,
            [
                """
                SELECT *
                FROM "dbo"."Orders"
                ORDER BY "dbo"."Orders"."Total" ASC, "dbo"."Orders"."Id" DESC
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.MySql,
            [
                """
                SELECT *
                FROM `dbo`.`Orders`
                ORDER BY `dbo`.`Orders`.`Total` ASC, `dbo`.`Orders`.`Id` DESC
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.Oracle,
            [
                """
                SELECT *
                FROM "dbo"."Orders"
                ORDER BY "dbo"."Orders"."Total" ASC, "dbo"."Orders"."Id" DESC
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.PostgreSql,
            [
                """
                SELECT *
                FROM "dbo"."Orders"
                ORDER BY "dbo"."Orders"."Total" ASC, "dbo"."Orders"."Id" DESC
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.SqLite,
            [
                """
                SELECT *
                FROM "dbo"."Orders"
                ORDER BY "dbo"."Orders"."Total" ASC, "dbo"."Orders"."Id" DESC
                """
            ]
        ),
        new SqlTestCase(
            SqlDialectKind.SqlServer,
            [
                """
                SELECT *
                FROM [dbo].[Orders]
                ORDER BY [dbo].[Orders].[Total] ASC, [dbo].[Orders].[Id] DESC
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

    public static TheoryData<SqlTestCase> OrderByErrorData
    {
        get {
            var index = 1;

            return [
                new SqlTestCase(
                    SqlDialectKind.CustomDb,
                    expectedExceptionType: typeof(ArgumentException),
                    expectedExceptionMessage: $"Property 'FakeColumn' not found on '{typeof(Product).Name}'.",
                    id: index++.ToString()
                ),
                new SqlTestCase(
                    SqlDialectKind.CustomDb,
                    expectedExceptionType: typeof(ArgumentException),
                    expectedExceptionMessage: $"Property 'UnmappedProperty' not found on '{typeof(OrderTestModel).Name}'.",
                    id: index++.ToString()
                )
            ];
        }
    }
}