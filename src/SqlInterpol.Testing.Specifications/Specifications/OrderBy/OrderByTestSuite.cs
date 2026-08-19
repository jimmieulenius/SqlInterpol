using System;
using System.Linq;
using SqlInterpol.Configuration;
using SqlInterpol.Schema;
using SqlInterpol.Segments;
using SqlInterpol.Testing.Xunit;

namespace SqlInterpol.Testing.Specifications;

[SqlTestSuite(typeof(IOrderByTestSuite))]
public abstract partial class OrderByTestSuite
{
    [SqlGeneratorIgnore]
    public abstract SqlBuilder CreateBuilder(SqlInterpolOptions? options = null);

    [SqlTest(nameof(IOrderByTestSuite.OrderByExpressionData))]
    public void OrderBy_EntityExpression(SqlTestCase testCase)
    {
        // Arrange
        var db = CreateBuilder();
        
        // Act
        testCase.Act(() => 
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

    [SqlTest(nameof(IOrderByTestSuite.OrderByCombinerData))]
    public void OrderBy_WithParamsCombiner(SqlTestCase testCase)
    {
        // Arrange
        var db = CreateBuilder();

        // Act
        testCase.Act(() => 
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

    [SqlTest(nameof(IOrderByTestSuite.OrderByEnumerableData))] // Bound to the correct explicit ASC dataset
    public void OrderBy_WithEnumerableCombiner(SqlTestCase testCase)
    {
        // Arrange
        var db = CreateBuilder();

        // Act - Simulates mapping an incoming string array directly from a Web API endpoint
        testCase.Act(() =>
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

    [SqlTest(nameof(IOrderByTestSuite.OrderByRawData))]
    public void OrderBy_WithSqlRaw(SqlTestCase testCase)
    {
        // Arrange
        var db = CreateBuilder();

        // Act
        testCase.Act(() => 
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

    [SqlTest(nameof(IOrderByTestSuite.OrderByMixedRawData))]
    public void OrderBy_MixingTypedAndRaw(SqlTestCase testCase)
    {
        // Arrange
        var db = CreateBuilder();

        // Act
        testCase.Act(() => 
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

    [SqlTest(nameof(IOrderByTestSuite.OrderByErrorData))]
    public void OrderBy_ValidationRules(SqlTestCase testCase)
    {
        // Act
        testCase.Act(() => 
        {
            #pragma warning disable SQLIG10
            var db = CreateBuilder();

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

        // Assert
        testCase.Assert();
    }
}