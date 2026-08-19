using SqlInterpol.Testing.Xunit;

namespace SqlInterpol.Testing.Specifications;

[SqlTestSuite(typeof(ILockTestSuite))]
public abstract partial class LockTestSuite
{
    [SqlGeneratorIgnore]
    public abstract SqlBuilder CreateBuilder();

    [SqlTest(nameof(ILockTestSuite.SelectWithForUpdateData))]
    public void Select_WithForUpdate(SqlTestCase testCase)
    {
        var db = CreateBuilder();
        int id = 5;

        testCase.Act(() => 
        {
            db.Entity<Product>(out var p);
            return db.Append($$"""
                SELECT {{p.Id}}, {{p.Name}}
                FROM {{p}} FOR UPDATE
                WHERE {{p.Id}} = {{id}}
                """).Build();
        });

        testCase.Assert();
        db.AssertAotIntercepted();
    }

    [SqlTest(nameof(ILockTestSuite.SelectWithForShareData))]
    public void Select_WithForShare(SqlTestCase testCase)
    {
        var db = CreateBuilder();
        int id = 5;

        testCase.Act(() => 
        {
            db.Entity<Product>(out var p);
            return db.Append($$"""
                SELECT {{p.Id}}, {{p.Name}}
                FROM {{p}} FOR SHARE
                WHERE {{p.Id}} = {{id}}
                """).Build();
        });

        testCase.Assert();
        db.AssertAotIntercepted();
    }
}