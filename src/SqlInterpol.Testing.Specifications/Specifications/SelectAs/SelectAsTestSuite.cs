using SqlInterpol.Configuration;
using SqlInterpol.Schema;
using SqlInterpol.Testing.Xunit;

namespace SqlInterpol.Testing.Specifications;

[SqlTestSuite(typeof(ISelectAsTestSuite))]
public abstract partial class SelectAsTestSuite
{
    [SqlGeneratorIgnore]
    public abstract SqlBuilder CreateBuilder(SqlInterpolOptions? options = null);

    [SqlTest(nameof(ISelectAsTestSuite.ProjectionAsLiteralData))]
    public void SelectAs_LiteralProjection(SqlTestCase testCase)
    {
        var db = CreateBuilder();
        
        testCase.Act(() => 
        {
            db.Entity<Product>(out var p);
            return db.Append($$"""
                SELECT
                    {{p.Id}} AS ProductId
                FROM {{p}}
                """).Build();
        });

        testCase.Assert();
    }

    [SqlTest(nameof(ISelectAsTestSuite.RawColumnAsProjectionData))]
    public void SelectAs_RawColumnProjection(SqlTestCase testCase)
    {
        var db = CreateBuilder();
        
        testCase.Act(() => 
        {
            db.Entity<Product>(out var p);

            return db.Append($$"""
                SELECT
                    {{p.Column("Id")}} AS {{"ProductId"}}
                FROM {{p}}
                """).Build();
        });

        testCase.Assert();
    }

    [SqlTest(nameof(ISelectAsTestSuite.ProjectionAsProjectionWithAttributeData))]
    public void SelectAs_ProjectionWithAttribute(SqlTestCase testCase)
    {
        var db = CreateBuilder();
        
        testCase.Act(() => 
        {
            db.Entity<Product>(out var p);
            return db.Append($$"""
                SELECT
                    {{p.Name}} AS {{p.Name}}
                FROM {{p}}
                """).Build();
        });

        testCase.Assert();
    }
}