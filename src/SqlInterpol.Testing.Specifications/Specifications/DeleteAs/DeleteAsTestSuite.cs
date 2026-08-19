using SqlInterpol.Configuration;
using SqlInterpol.Testing.Xunit;

namespace SqlInterpol.Testing.Specifications;

[SqlTestSuite(typeof(IDeleteAsTestSuite))]
public abstract partial class DeleteAsTestSuite
{
    [SqlGeneratorIgnore]
    public abstract SqlBuilder CreateBuilder(SqlInterpolOptions? options = null);

    protected const int TargetId = 42;

    [SqlTest(nameof(IDeleteAsTestSuite.DeleteWithExplicitAliasData))]
    public void Delete_WithExplicitAlias(SqlTestCase testCase)
    {
        // Arrange
        var db = CreateBuilder();

        // Act
        testCase.Act(() => 
        {
            db.Entity<OrderModel>(out var o);
            
            return db.Append($$"""
                DELETE FROM {{o}} AS {{"o"}}
                WHERE {{o.Id}} = {{TargetId}}
                """).Build();
        });

        // Assert
        testCase.Assert();
        db.AssertAotIntercepted();
    }

    [SqlTest(nameof(IDeleteAsTestSuite.DeleteWithoutAliasData))]
    public void Delete_WithoutAlias_StripsAutoAlias(SqlTestCase testCase)
    {
        // Arrange
        var db = CreateBuilder();
        db.Context.Options.EntityAutoAliasing = true; // Even with auto-aliasing enabled, it strips safely!

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
}