using SqlInterpol.Configuration;
using SqlInterpol.Testing.Xunit;

namespace SqlInterpol.Testing.Specifications;

[SqlTestSuite(typeof(IHavingTestSuite))]
public abstract partial class HavingTestSuite
{
    [SqlGeneratorIgnore]
    public abstract SqlBuilder CreateBuilder(SqlInterpolOptions? options = null);

    [SqlTest(nameof(IHavingTestSuite.SelectGroupByAndHavingData))]
    public void Select_GroupByAndHaving(SqlTestCase testCase)
    {
        // Arrange
        var db = CreateBuilder();

        // Act
        testCase.Act(() =>
        {
            db.Entity<Product>(out var p);
            return db.Append($$"""
                SELECT 
                    {{p.CategoryId}},
                    COUNT({{p.Id}}) AS ProductCount
                FROM {{p}}
                GROUP BY {{p.CategoryId}}
                HAVING COUNT({{p.Id}}) > {{5}}
                """).Build();
        });

        // Assert
        testCase.Assert();
        db.AssertAotIntercepted();
    }
}