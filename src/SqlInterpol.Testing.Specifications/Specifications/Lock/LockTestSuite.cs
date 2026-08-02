using SqlInterpol.Schema;
using SqlInterpol.Testing.Xunit;

namespace SqlInterpol.Testing.Specifications;

public abstract class LockTestSuite
{
    [SqlIgnoreMember]
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

    [SqlTable(name: "Products", schema: "dbo")]
    public class Product
    {
        public int Id { get; set; }
        [SqlColumn("PROD_NAME")]
        public string Name { get; set; } = null!;
        public bool IsActive { get; set; }
        public int CategoryId { get; set; }
        public decimal Price { get; set; }
    }
}