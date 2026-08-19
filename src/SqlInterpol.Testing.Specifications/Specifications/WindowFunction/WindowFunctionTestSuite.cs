using SqlInterpol.Configuration;
using SqlInterpol.Testing.Xunit;

namespace SqlInterpol.Testing.Specifications;

[SqlTestSuite(typeof(IWindowFunctionTestSuite))]
public abstract partial class WindowFunctionTestSuite
{
    [SqlGeneratorIgnore]
    public abstract SqlBuilder CreateBuilder(SqlInterpolOptions? options = null);

    [SqlTest(nameof(IWindowFunctionTestSuite.WindowFunctionData))]
    public void Select_WindowFunction_Wysiwyg(SqlTestCase testCase)
    {
        var db = CreateBuilder();

        testCase.Act(() => 
        {
            return db.Entity<Product>(out var p)
                .Append($$"""
                SELECT
                    {{p.Name}},
                    SUM({{p.Price}}) OVER (
                        PARTITION BY {{p.CategoryId}}
                        ORDER BY {{p.Id}} DESC
                    ) AS CategoryTotal
                FROM {{p}}
                """).Build();
        });

        testCase.Assert();
        db.AssertAotIntercepted();
    }

    [SqlTest(nameof(IWindowFunctionTestSuite.RawWindowFunctionData))]
    public void Select_RawWindowFunction(SqlTestCase testCase)
    {
        var db = CreateBuilder();

        testCase.Act(() => 
        {
            return db.Entity<Product>(out var p)
                .Append($$"""
                SELECT 
                    {{p.Name}},
                    {{p.Price}},
                    AVG({{p.Price}}) OVER (PARTITION BY {{p.CategoryId}}) AS AvgCategoryPrice
                FROM {{p}}
                """).Build();
        });

        testCase.Assert();
        db.AssertAotIntercepted();
    }
}