using SqlInterpol.Configuration;
using SqlInterpol.Testing.Xunit;

namespace SqlInterpol.Testing.Specifications;

[SqlTestSuite(typeof(IUpdateAsTestSuite))]
public abstract partial class UpdateAsTestSuite
{
    [SqlGeneratorIgnore]
    public abstract SqlBuilder CreateBuilder(SqlInterpolOptions? options = null);

    [SqlTest(nameof(IUpdateAsTestSuite.UpdateSetWithAliasData))]
    public void Update_WithExplicitAlias(SqlTestCase testCase)
    {
        var db = CreateBuilder();
        var updateDto = new { Status = "Shipped", Total = 99.99m };
        
        testCase.Act(() => 
        {
            db.Entity<OrderModel>(out var o);

            return db.Append($$"""
                UPDATE {{o}} AS {{"ord"}}
                SET {{updateDto}}
                WHERE {{o.Id}} = 1
                """).Build();
        });

        testCase.Assert();
    }
}