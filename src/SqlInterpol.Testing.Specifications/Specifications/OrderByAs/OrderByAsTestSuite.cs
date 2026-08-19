using SqlInterpol.Configuration;
using SqlInterpol.Testing.Xunit;

namespace SqlInterpol.Testing.Specifications;

[SqlTestSuite(typeof(IOrderByAsTestSuite))]
public abstract partial class OrderByAsTestSuite
{
    [SqlGeneratorIgnore]
    public abstract SqlBuilder CreateBuilder(SqlInterpolOptions? options = null);

    [SqlTest(nameof(IOrderByAsTestSuite.OrderByWithExplicitAliasData))]
    public void OrderBy_WithExplicitAlias(SqlTestCase testCase)
    {
        // Arrange
        var db = CreateBuilder();

        // Act
        testCase.Act(() => 
        {
            db.Entity<Product>(out var prod, "prod");
            return db.Append($$"""
                SELECT *
                FROM {{prod}} AS {{prod:alias}}
                ORDER BY
                    {{prod.Name}} ASC
                """).Build();
        });

        // Assert
        testCase.Assert();
        db.AssertAotIntercepted();
    }
}