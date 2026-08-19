using SqlInterpol.Configuration;
using SqlInterpol.Testing.Xunit;

namespace SqlInterpol.Testing.Specifications;

[SqlTestSuite(typeof(IInsertSubqueryTestSuite))]
public abstract partial class InsertSubqueryTestSuite
{
    [SqlGeneratorIgnore]
    public abstract SqlBuilder CreateBuilder(SqlInterpolOptions? options = null);

    [SqlTest(nameof(IInsertSubqueryTestSuite.InsertSelectData))]
    public void Insert_WithSelectSubquery(SqlTestCase testCase)
    {
        // Arrange
        var db = CreateBuilder();
        var targetId = 100;
        
        // Act
        // Insert into OrderModel (Total) using OrderLine (Quantity)
        testCase.Act(() => 
        {
            db.Entity<OrderModel>(out var o)
              .Entity<OrderLine>(out var l);

            return db.Append($$"""
                INSERT INTO {{o}} 
                ({{o.Id}}, {{o.Total}})
                SELECT {{l.OrderId}}, {{l.Quantity}}
                FROM {{l}}
                WHERE {{l.OrderId}} = {{targetId}}
                """).Build();
        });

        // Assert
        testCase.Assert();
        db.AssertAotIntercepted();
    }
}