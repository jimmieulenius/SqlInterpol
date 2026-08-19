using SqlInterpol.Configuration;
using SqlInterpol.Testing.Xunit;

namespace SqlInterpol.Testing.Specifications;

[SqlTestSuite(typeof(IGroupBySubqueryTestSuite))]
public abstract partial class GroupBySubqueryTestSuite
{
    [SqlGeneratorIgnore]
    public abstract SqlBuilder CreateBuilder(SqlInterpolOptions? options = null);

    [SqlTest(nameof(IGroupBySubqueryTestSuite.GroupBySubqueryData))]
    public void GroupBy_AgainstSubquery(SqlTestCase testCase)
    {
        // Arrange
        var db = CreateBuilder();
        
        // Act
        testCase.Act(() =>
        {
            db.Entity<StatsModel>(out var stats, "stats");
            db.Query(stats, out var statsQuery, () => db.Append($$"""
                SELECT CategoryId, MAX(Price) AS MaxPrice FROM Products GROUP BY CategoryId
                """));

            return db.Append($$"""
                SELECT CategoryId, COUNT(*)
                FROM {{stats:decl}}
                GROUP BY {{stats.CategoryId}}
                """).Build();
        });

        // Assert
        testCase.Assert();
        db.AssertAotIntercepted();
    }
}