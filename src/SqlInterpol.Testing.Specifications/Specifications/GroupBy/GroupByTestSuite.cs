using System.Linq;
using SqlInterpol.Configuration;
using SqlInterpol.Schema;
using SqlInterpol.Testing.Xunit;

namespace SqlInterpol.Testing.Specifications;

[SqlTestSuite(typeof(IGroupByTestSuite))]
public abstract partial class GroupByTestSuite
{
    [SqlGeneratorIgnore]
    public abstract SqlBuilder CreateBuilder(SqlInterpolOptions? options = null);

    [SqlTest(nameof(IGroupByTestSuite.GroupByCombinerData))]
    public void GroupBy_EntityExpression(SqlTestCase testCase)
    {
        // Arrange
        var db = CreateBuilder();
        
        // Act - Uses zero-allocation POCO property routing
        testCase.Act(() =>
        {
            db.Entity<OrderModel>(out var o);
            return db.Append($$"""
            SELECT CategoryId, order_status, COUNT(*)
            FROM {{o}}
            GROUP BY {{o.CategoryId}}, {{o.Status}}
            """).Build();
        });

        // Assert
        testCase.Assert();
        db.AssertAotIntercepted();
    }

    [SqlTest(nameof(IGroupByTestSuite.GroupByCombinerData))]
    public void GroupBy_WithEnumerableCombiner(SqlTestCase testCase)
    {
        // Arrange
        var db = CreateBuilder();
        // Simulate generating fragments dynamically from an API request using C# property names
        string[] apiRequestFields = ["CategoryId", "Status"];

        // Act - Testing dynamic API scenario using LINQ Select and the Column extension
        testCase.Act(() =>
        {
            db.Entity<OrderModel>(out var o);
            
#pragma warning disable SQLIG10
            return db.Append($$"""
            SELECT CategoryId, order_status, COUNT(*)
            FROM {{o}}
            GROUP BY {{apiRequestFields.Select(f => o.Column(f))}}
            """).Build();
#pragma warning restore SQLIG10
        });

        // Assert
        testCase.Assert();
    }

    [SqlTest(nameof(IGroupByTestSuite.GroupByWithSqlRawData))]
    public void GroupBy_WithSqlRaw(SqlTestCase testCase)
    {
        // Arrange
        var db = CreateBuilder();

        // Act
        testCase.Act(() =>
        {
            db.Entity<OrderModel>(out var o);

#pragma warning disable SQLIG10
            return db.Append($$"""
            SELECT YEAR(created_at), COUNT(*)
            FROM {{o}}
            GROUP BY {{Sql.Raw("YEAR(created_at)")}}
            """).Build();
#pragma warning restore SQLIG10
        });

        // Assert
        testCase.Assert();
    }

    [SqlTest(nameof(IGroupByTestSuite.GroupByMixingTypedAndRawData))]
    public void GroupBy_MixingTypedAndRaw(SqlTestCase testCase)
    {
        // Arrange
        var db = CreateBuilder();

        // Act
        testCase.Act(() =>
        {
            db.Entity<OrderModel>(out var o);

#pragma warning disable SQLIG10
            return db.Append($$"""
            SELECT order_status, YEAR(created_at), COUNT(*)
            FROM {{o}}
            GROUP BY {{o.Status}}, {{Sql.Raw("YEAR(created_at)")}}
            """).Build();
#pragma warning restore SQLIG10
        });

        // Assert
        testCase.Assert();
    }
}