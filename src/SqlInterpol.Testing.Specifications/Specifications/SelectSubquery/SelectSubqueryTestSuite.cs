using SqlInterpol.Configuration;
using SqlInterpol.Execution;
using SqlInterpol.Schema;
using SqlInterpol.Testing.Xunit;

// Route the generated code back to these exact nested classes
using Product = SqlInterpol.Testing.Specifications.SelectSubqueryTestSuite.Product;
using Category = SqlInterpol.Testing.Specifications.SelectSubqueryTestSuite.Category;

namespace SqlInterpol.Testing.Specifications;

[SqlTestSuite(typeof(ISelectSubqueryTestSuite))]
public abstract partial class SelectSubqueryTestSuite
{
    [SqlGeneratorIgnore]
    public abstract SqlBuilder CreateBuilder(SqlInterpolOptions? options = null);

    [SqlTest(nameof(ISelectSubqueryTestSuite.SelectSubqueryData))]
    public void Select_WithInlineSubquery(SqlTestCase testCase)
    {
        var activeStatus = 100;
        var minPrice = 101;

        var db1 = CreateBuilder();
        var db2 = CreateBuilder();
        
        testCase.Act(() => 
        {
            db1.Entity<Product>(out var p1)
               .Entity<Category>(out var c1)
               .Query(c1, out var categorySubquery1, () => db1.Append($$"""
                   SELECT
                       {{c1.Name}}
                   FROM {{c1}}
                   WHERE {{c1.Id}} = {{p1.CategoryId}} AND {{c1.IsActive}} = {{activeStatus}}
                   """));

#pragma warning disable SQLIG10
            var result1 = db1.Append($$"""
                SELECT 
                    {{p1.Id}},
                    (
                        {{categorySubquery1}}
                    ) AS CategoryName
                FROM {{p1}} AS prod
                WHERE {{p1.Price}} > {{minPrice}}
                """).Build();
#pragma warning restore SQLIG10

            db2.Entity<Product>(out var p2)
               .Entity<Category>(out var c2)
               .Query(c2, out var categorySubquery2, () => db2.Append($$"""
                   SELECT
                       {{c2.Name}}
                   FROM {{c2}}
                   WHERE {{c2.Id}} = {{p2.CategoryId}} AND {{c2.IsActive}} = {{activeStatus}}
                   """));

#pragma warning disable SQLIG10
            var result2 = db2.Append($$"""
                SELECT 
                    {{p2.Id}},
                    (
                        {{categorySubquery2}}
                    ) AS CategoryName
                FROM {{p2}} AS second_prod
                WHERE {{p2.Price}} > {{minPrice}}
                """).Build();
#pragma warning restore SQLIG10

            return [result1, result2];
        });

        testCase.Assert();
    }

    [SqlTest(nameof(ISelectSubqueryTestSuite.SelectSubqueryData))]
    public void Select_WithFunctionSubquery(SqlTestCase testCase)
    {
        var activeStatus = 100;
        var minPrice = 101;

        var db1 = CreateBuilder();     
        db1.Entity<Product>(out var p1);
        
        var db2 = CreateBuilder();
        db2.Entity<Product>(out var p2);

        testCase.Act(() =>
        {
#pragma warning disable SQLIG10
            return [
                db1.Append($$"""
                    SELECT 
                        {{p1.Id}},
                        (
                            {{db1.BuildCategorySubquery(p1, activeStatus)}}
                        ) AS CategoryName
                    FROM {{p1}} AS prod
                    WHERE {{p1.Price}} > {{minPrice}}
                    """).Build(),

                db2.Append($$"""
                    SELECT 
                        {{p2.Id}},
                        (
                            {{db2.BuildCategorySubquery(p2, activeStatus)}}
                        ) AS CategoryName
                    FROM {{p2}} AS second_prod
                    WHERE {{p2.Price}} > {{minPrice}}
                    """).Build()
            ];
#pragma warning restore SQLIG10
        });

        testCase.Assert();
    }
}