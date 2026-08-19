using SqlInterpol.Configuration;
using SqlInterpol.Testing.Xunit;

namespace SqlInterpol.Testing.Specifications;

[SqlTestSuite(typeof(IGroupByAsTestSuite))]
public abstract partial class GroupByAsTestSuite
{
    [SqlGeneratorIgnore]
    public abstract SqlBuilder CreateBuilder(SqlInterpolOptions? options = null);

    [SqlTest(nameof(IGroupByAsTestSuite.GroupByWithExplicitAliasData))]
    public void GroupBy_WithExplicitAlias(SqlTestCase testCase)
    {
        // Arrange
        var db = CreateBuilder();

        // Act
        testCase.Act(() =>
        {
            db.Entity<Product>(out var p);
            return db.Append($$"""
                SELECT {{p.Name}}, {{p.IsActive}}, COUNT(*)
                FROM {{p}} AS prod
                GROUP BY {{p.Name}}, {{p.IsActive}}
                """).Build();
        });

        // Assert
        testCase.Assert();
        db.AssertAotIntercepted();
    }
}