using SqlInterpol.Configuration;
using SqlInterpol.Testing.Xunit;

namespace SqlInterpol.Testing.Specifications;

[SqlTestSuite(typeof(IWhereTestSuite))]
public abstract partial class WhereTestSuite
{
    [SqlGeneratorIgnore]
    public abstract SqlBuilder CreateBuilder(SqlInterpolOptions? options = null);

    [SqlTest(nameof(IWhereTestSuite.WhereSimpleParameterData))]
    public void Where_SimpleParameter(SqlTestCase testCase)
    {
        var db = CreateBuilder();
        int targetId = 42;

        testCase.Act(() => 
        {
            db.Entity<Product>(out var p);
            return db.Append($$"""
                SELECT
                    {{p.Id}}
                FROM {{p}}
                WHERE {{p.Id}} = {{targetId}}
                """).Build();
        });

        testCase.Assert();
        db.AssertAotIntercepted();
    }

    [SqlTest(nameof(IWhereTestSuite.WhereInCollectionData))]
    public void Where_InCollection(SqlTestCase testCase)
    {
        var db = CreateBuilder();
        int[] categoryIds = [10, 20, 30];

        testCase.Act(() => 
        {
            db.Entity<Product>(out var p);

            return db.Append($$"""
                SELECT
                    {{p.Id}}
                FROM {{p}}
                WHERE {{p.CategoryId}} IN ({{categoryIds}})
                """).Build();
        });

        testCase.Assert();
        db.AssertAotIntercepted();
    }
}