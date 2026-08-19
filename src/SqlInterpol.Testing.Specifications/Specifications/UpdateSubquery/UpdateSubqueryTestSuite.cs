using SqlInterpol.Configuration;
using SqlInterpol.Schema;
using SqlInterpol.Testing.Xunit;

namespace SqlInterpol.Testing.Specifications;

[SqlTestSuite(typeof(IUpdateSubqueryTestSuite))]
public abstract partial class UpdateSubqueryTestSuite
{
    protected const decimal TargetMaxPrice = 99.99m;

    [SqlGeneratorIgnore]
    public abstract SqlBuilder CreateBuilder(SqlInterpolOptions? options = null);

    [SqlTest(nameof(IUpdateSubqueryTestSuite.UpdateSubqueryData))]
    public void Update_AgainstRawSubquery(SqlTestCase testCase)
    {
        var db = CreateBuilder();
        var updateDto = new { MaxPrice = TargetMaxPrice };
        
        testCase.Act(() => 
        {
            db.Entity<OrderStatsModel>(out var stats);

#pragma warning disable SQLIG10
            return db.Append($$"""
                UPDATE (
                    {{db.Query(stats, () => db.Append($$"""
                        SELECT CategoryId, MAX(Price) AS max_price FROM Products GROUP BY CategoryId
                        """))}}
                ) AS {{"stats"}}
                SET {{updateDto}}
                WHERE {{stats.CategoryId}} = 5
                """).Build();
#pragma warning restore SQLIG10
        });

        testCase.Assert();
    }

    [SqlTest(nameof(IUpdateSubqueryTestSuite.UpdateTypeSafeSubqueryData))]
    public void Update_AgainstTypeSafeSubquery(SqlTestCase testCase)
    {
        var db = CreateBuilder();
        var updateDto = new { MaxPrice = TargetMaxPrice };

        testCase.Act(() => 
        {
            db.Entity<OrderStatsModel>(out var stats)
              .Entity<Product>(out var p);

#pragma warning disable SQLIG10
            return db.Append($$"""
                UPDATE (
                    {{db.Query(stats, () => db.Append($$"""
                        SELECT
                            {{p.CategoryId}} AS {{stats.CategoryId}},
                            MAX({{p.Price}}) AS {{stats.MaxPrice}}
                        FROM {{p}} AS {{"p"}}
                        GROUP BY {{p.CategoryId}}
                        """))}}
                ) AS {{"stats"}}
                SET {{updateDto}}
                WHERE {{stats.CategoryId}} = 5
                """).Build();
#pragma warning restore SQLIG10
        });

        testCase.Assert();
    }
}