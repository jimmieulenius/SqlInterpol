using SqlInterpol.Configuration;
using SqlInterpol.Testing.Xunit;

namespace SqlInterpol.Testing.Specifications;

[SqlTestSuite(typeof(IOrderBySubqueryTestSuite))]
public abstract partial class OrderBySubqueryTestSuite
{
    [SqlGeneratorIgnore]
    public abstract SqlBuilder CreateBuilder(SqlInterpolOptions? options = null);

    [SqlTest(nameof(IOrderBySubqueryTestSuite.OrderBySubqueryData))]
    public void OrderBy_AgainstSubquery(SqlTestCase testCase)
    {
        // Arrange
        var db = CreateBuilder();
        
        // Act
        testCase.Act(() => 
        {
            db.Entity<StatsModel>(out var stats, "stats");
            return db.Append($$"""
                SELECT *
                FROM
                (
                    SELECT
                        CategoryId,
                        MAX(Price) AS MaxPrice
                    FROM Products
                    GROUP BY CategoryId
                ) AS {{stats:alias}}
                ORDER BY {{stats.MaxPrice}} DESC
                """).Build();
        });

        // Assert
        testCase.Assert();
        db.AssertAotIntercepted();
    }
}