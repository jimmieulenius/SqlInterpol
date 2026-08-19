using SqlInterpol.Configuration;
using SqlInterpol.Testing.Xunit;

namespace SqlInterpol.Testing.Specifications;

[SqlTestSuite(typeof(IJoinAsTestSuite))]
public abstract partial class JoinAsTestSuite
{
    [SqlGeneratorIgnore]
    public abstract SqlBuilder CreateBuilder(SqlInterpolOptions? options = null);

    [SqlTest(nameof(IJoinAsTestSuite.JoinWithLiteralAliasesData))]
    public void Join_WithLiteralAliases(SqlTestCase testCase)
    {
        // Arrange
        var db = CreateBuilder();
        
        // Act
        testCase.Act(() => 
        {
            db.Entity<Product>(out var p)
              .Entity<OrderLine>(out var ol);

            return db.Append($$"""
                SELECT
                    {{p.Id}},
                    {{ol.OrderId}}
                FROM {{p}} AS p
                JOIN {{ol}} AS ol
                    ON {{p.Id}} = {{ol.ProductItemNumber}}
                """).Build();
        });

        // Assert
        testCase.Assert();
        db.AssertAotIntercepted();
    }

    [SqlTest(nameof(IJoinAsTestSuite.JoinWithExplicitApiAliasesData))]
    public void Join_WithExplicitApiAliases(SqlTestCase testCase)
    {
        // Arrange
        var db = CreateBuilder();
        
        // Act
        testCase.Act(() => 
        {
            db.Entity<Product>(out var prod, "prod")
              .Entity<OrderLine>(out var OrderLine, "OrderLine");

            return db.Append($$"""
                SELECT
                    {{prod.Id}},
                    {{OrderLine.OrderId}}
                FROM dbo.Products AS {{prod:alias}}
                JOIN order_lines AS {{OrderLine:alias}}
                    ON {{prod.Id}} = {{OrderLine.ProductItemNumber}}
                """).Build();
        });

        // Assert
        testCase.Assert();
        db.AssertAotIntercepted();
    }

    [SqlTest(nameof(IJoinAsTestSuite.SelfJoinData))]
    public void Join_SelfJoin(SqlTestCase testCase)
    {
        // Arrange
        var db = CreateBuilder();

        // Act
        // The preprocessor naturally detects "AS original" and "AS related" 
        // and wires them automatically to the p1 and p2 scopes!
        testCase.Act(() => 
        {
            db.Entity<Product>(out var p1)
              .Entity<Product>(out var p2);

            return db.Append($$"""
                SELECT
                    {{p1.Id}},
                    {{p2.Id}}
                FROM {{p1}} AS original
                JOIN {{p2}} AS related
                    ON {{p1.CategoryId}} = {{p2.CategoryId}}
                """).Build();
        });

        // Assert
        testCase.Assert();
        db.AssertAotIntercepted();
    }

    [SqlTest(nameof(IJoinAsTestSuite.JoinWithConfigOverrideData))]
    public void Join_WithSqlEntityConfig(SqlTestCase testCase)
    {
        // Arrange
        var db = CreateBuilder();
        
        // Act
        // Brilliant API experience! The metadata overrides happen securely at declaration time!
        testCase.Act(() => 
        {
            db.Entity<Product>(out var p, name: "Archive_Products", schema: "history")
              .Entity<OrderLine>(out var ol);

            return db.Append($$"""
                SELECT
                    {{p.Id}},
                    {{ol.OrderId}}
                FROM {{p}}
                JOIN {{ol}}
                    ON {{p.Id}} = {{ol.ProductItemNumber}}
                """).Build();
        });

        // Assert
        testCase.Assert();
        db.AssertAotIntercepted();
    }
}