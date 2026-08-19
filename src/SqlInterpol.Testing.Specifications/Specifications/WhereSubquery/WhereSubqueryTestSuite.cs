using SqlInterpol.Configuration;
using SqlInterpol.Testing.Xunit;

namespace SqlInterpol.Testing.Specifications;

[SqlTestSuite(typeof(IWhereSubqueryTestSuite))]
public abstract partial class WhereSubqueryTestSuite
{
    [SqlGeneratorIgnore]
    public abstract SqlBuilder CreateBuilder(SqlInterpolOptions? options = null);

    [SqlTest(nameof(IWhereSubqueryTestSuite.WhereInSubqueryData))]
    public void Where_In_Subquery(SqlTestCase testCase)
    {
        var db = CreateBuilder();

        testCase.Act(() => 
        {
            // 1. Declare Product and build the subquery fragment
            db.Entity<Product>(out var p);
            var activeCategoriesQuery = db.Fragment(f => f.Append($$"""
                SELECT 
                    {{p.CategoryId}}
                FROM {{p}} AS p
                WHERE {{p.Price}} > 0
                """));

            // 2. Declare Category and build the main query, injecting the fragment natively
            db.Entity<Category>(out var c);

#pragma warning disable SQLIG10
            return db.Append($$"""
                SELECT 
                    {{c.Name}}
                FROM {{c}} AS c
                WHERE {{c.Id}} IN
                (
                    {{activeCategoriesQuery}}
                )
                """).Build();
#pragma warning restore SQLIG10
        });

        testCase.Assert();
    }
}