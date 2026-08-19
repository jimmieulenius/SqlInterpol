using SqlInterpol.Configuration;
using SqlInterpol.Testing.Xunit;

namespace SqlInterpol.Testing.Specifications;

[SqlTestSuite(typeof(IWhereAsTestSuite))]
public abstract partial class WhereAsTestSuite
{
    [SqlGeneratorIgnore]
    public abstract SqlBuilder CreateBuilder(SqlInterpolOptions? options = null);

    [SqlTest(nameof(IWhereAsTestSuite.WhereWithAliasedEntityData))]
    public void Where_WithAliasedEntityAndColumn(SqlTestCase testCase)
    {
        var db = CreateBuilder();
        var productId = 1;
        var categoryId = 5;

        testCase.Act(() => 
        {
            db.Entity<Product>(out var p);
            return db.Append($$"""
                SELECT
                    {{p.Id}} AS ProductId
                FROM {{p}} AS p
                WHERE {{p.Id}} = {{productId}} AND {{p.CategoryId}} = {{categoryId}}
                """).Build();
        });

        testCase.Assert();
        db.AssertAotIntercepted();
    }
}