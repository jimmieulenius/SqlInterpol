using SqlInterpol.Configuration;
using SqlInterpol.Testing.Xunit;

namespace SqlInterpol.Testing.Specifications;

[SqlTestSuite(typeof(IJoinTestSuite))]
public abstract partial class JoinTestSuite
{
    [SqlGeneratorIgnore]
    public abstract SqlBuilder CreateBuilder(SqlInterpolOptions? options = null);

    [SqlTest(nameof(IJoinTestSuite.JoinTwoEntitiesData))]
    public void Join_TwoEntities(SqlTestCase testCase)
    {
        // Arrange
        var db = CreateBuilder();
        
        // Act - Uses fluent entity initialization and zero-allocation properties
        testCase.Act(() => 
        {
            db.Entity<Product>(out var p)
              .Entity<OrderLine>(out var o);

            return db.Append($$"""
                SELECT
                    {{p.Id}},
                    {{o.OrderId}}
                FROM {{p}}
                JOIN {{o}}
                    ON {{p.Id}} = {{o.ProductItemNumber}}
                """).Build();
        });

        // Assert
        testCase.Assert();
        db.AssertAotIntercepted();
    }
}