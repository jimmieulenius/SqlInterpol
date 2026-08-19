using SqlInterpol.Configuration;
using SqlInterpol.Schema;
using SqlInterpol.Testing.Xunit;

namespace SqlInterpol.Testing.Specifications;

[SqlTestSuite(typeof(ISelectIntoTestSuite))]
public abstract partial class SelectIntoTestSuite
{
    [SqlGeneratorIgnore]
    public abstract SqlBuilder CreateBuilder(SqlInterpolOptions? options = null);

    [SqlTest(nameof(ISelectIntoTestSuite.SelectIntoData))]
    public void Select_IntoNewTable(SqlTestCase testCase)
    {
        var db = CreateBuilder();

        testCase.Act(() => 
        {
            db.Entity<Product>(out var p);
            return db.Append($$"""
                SELECT {{p.Id}}, {{p.Name}}
                INTO #TempProducts
                FROM {{p}}
                """).Build();
        });

        testCase.Assert();
    }

    [SqlTest(nameof(ISelectIntoTestSuite.SelectIntoParameterizedData))]
    public void Select_IntoNewTable_WithParameterizedTarget(SqlTestCase testCase)
    {
        var db = CreateBuilder();
        var target = Sql.Raw("#TempProducts");

        testCase.Act(() => 
        {
            db.Entity<Product>(out var p);

#pragma warning disable SQLIG10
            return db.Append($$"""
                SELECT {{p.Id}}, {{p.Name}}
                INTO {{target}}
                FROM {{p}}
                """).Build();
#pragma warning restore SQLIG10
        });

        testCase.Assert();
    }
}