using SqlInterpol.Schema;
using SqlInterpol.Testing.Xunit;
using Xunit;

namespace SqlInterpol.Testing.Specifications;

public abstract class LockTestSuiteBase<T> : SqlTestSuiteBase<T>
{
    [Theory]
    [MemberData(nameof(SelectWithForUpdateData))]
    public void Select_WithForUpdate(SqlTestCase testCase)
    {
        // Arrange
        var db = CreateBuilder();
        int id = 5;

        // Act
        testCase.Act(() => 
        {
            db.Entity<Product>(out var p);
            return db.Append($$"""
                SELECT {{p.Id}}, {{p.Name}}
                FROM {{p}} FOR UPDATE
                WHERE {{p.Id}} = {{id}}
                """).Build();
        });

        // Assert
        testCase.Assert();

        if (SelectWithForUpdateTheory?.AotCompatible ?? false)
        {
            db.AssertAotIntercepted();
        }
    }

    [Theory]
    [MemberData(nameof(SelectWithForShareData))]
    public void Select_WithForShare(SqlTestCase testCase)
    {
        // Arrange
        var db = CreateBuilder();
        int id = 5;

        // Act
        testCase.Act(() => 
        {
            db.Entity<Product>(out var p);
            return db.Append($$"""
                SELECT {{p.Id}}, {{p.Name}}
                FROM {{p}} FOR SHARE
                WHERE {{p.Id}} = {{id}}
                """).Build();
        });

        // Assert
        testCase.Assert();

        if (SelectWithForShareTheory?.AotCompatible ?? false)
        {
            db.AssertAotIntercepted();
        }
    }

    public static SqlTestTheory? SelectWithForUpdateTheory { get; set; }

    public static TheoryData<SqlTestCase>? SelectWithForUpdateData => SelectWithForUpdateTheory?.Data;

    public static SqlTestTheory? SelectWithForShareTheory { get; set; }

    public static TheoryData<SqlTestCase>? SelectWithForShareData => SelectWithForShareTheory?.Data;

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