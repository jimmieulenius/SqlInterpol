using SqlInterpol.Configuration;
using SqlInterpol.Testing.Xunit;

namespace SqlInterpol.Testing.Specifications;

[SqlTestSuite(typeof(IDeleteTestSuite))]
public abstract partial class DeleteTestSuite
{
    [SqlGeneratorIgnore]
    public abstract SqlBuilder CreateBuilder(SqlInterpolOptions? options = null);

    protected const int TargetId = 42;
    // protected static readonly TestUser TemplateUser = new() { Id = 1, Name = "Bob", Age = 31 };

    [SqlTest(nameof(IDeleteTestSuite.DeletePureManualData))]
    public void Delete_PureManual(SqlTestCase testCase)
    {
        // Arrange
        var db = CreateBuilder();

        // Act
        testCase.Act(() => 
        {
            db.Entity<OrderModel>(out var o);
            return db.Append($$"""
                DELETE FROM {{o}}
                WHERE {{o.Id}} = {{TargetId}}
                """).Build();
        });

        // Assert
        testCase.Assert();
        db.AssertAotIntercepted();
    }

    [SqlTest(nameof(IDeleteTestSuite.DeleteMultiTableData))]
    public void Delete_MultiTable_TranslatesAcrossDialects(SqlTestCase testCase)
    {
        // Arrange
        var db = CreateBuilder();

        // Act
        testCase.Act(() => 
        {
            db.Entity<Product>(out var p).Entity<Category>(out var c);
            return db.Append($$"""
                DELETE FROM {{p}}
                FROM {{c}} AS c1
                WHERE {{p.CategoryId}} = c1.Id
                """).Build();
        });

        // Assert
        testCase.Assert();
        db.AssertAotIntercepted();
    }
}