using SqlInterpol.Configuration;
using SqlInterpol.Testing.Xunit;

namespace SqlInterpol.Testing.Specifications;

[SqlTestSuite(typeof(IFromAsTestSuite))]
public abstract partial class FromAsTestSuite
{
    [SqlGeneratorIgnore]
    public abstract SqlBuilder CreateBuilder(SqlInterpolOptions? options = null);

    [SqlTest(nameof(IFromAsTestSuite.From_EntityManualAliasData))]
    public void From_EntityManualAlias(SqlTestCase testCase)
    {
        // Arrange
        var db = CreateBuilder();

        // Act
        testCase.Act(() =>
        {
            db.Entity<Product>(out var p);
            return db.Append($$"""
                SELECT
                    {{p.Id}}
                FROM {{p}} AS p
                """).Build();
        });

        // Assert
        testCase.Assert();
        db.AssertAotIntercepted();
    }

    [SqlTest(nameof(IFromAsTestSuite.From_EntitySqlTableAttributeData))]
    public void From_EntitySqlTableAttribute(SqlTestCase testCase)
    {
        // Arrange
        var db = CreateBuilder();

        // Act
        testCase.Act(() =>
        {
            db.Entity<Product>(out var p);
            return db.Append($$"""
                SELECT
                    {{p.Id}}
                FROM {{p}} AS prod
                """).Build();
        });

        // Assert
        testCase.Assert();
        db.AssertAotIntercepted();
    }

    [SqlTest(nameof(IFromAsTestSuite.From_LiteralTableAsEntityWithoutAttributeData))]
    public void From_LiteralTableAsEntityWithoutAttribute(SqlTestCase testCase)
    {
        // Arrange
        var db = CreateBuilder();

        // Act
        testCase.Act(() =>
        {
            db.Entity<OrderLine>(out var ol);
            return db.Append($$"""
                SELECT
                    {{ol.OrderId}}
                FROM ORDER_LINES AS {{ol:alias}}
                """).Build();
        });

        // Assert
        testCase.Assert();
        db.AssertAotIntercepted();
    }

    [SqlTest(nameof(IFromAsTestSuite.From_LiteralTableAsExplicitAliasedEntityData))]
    public void From_LiteralTableAsExplicitAliasedEntity(SqlTestCase testCase)
    {
        // Arrange
        var db = CreateBuilder();

        // Act
        testCase.Act(() =>
        {
            db.Entity<Product>(out var p, "prod");
            return db.Append($$"""
                SELECT
                    {{p.Id}}
                FROM products AS {{p:alias}}
                """).Build();
        });

        // Assert
        testCase.Assert();
        db.AssertAotIntercepted();
    }

    [SqlTest(nameof(IFromAsTestSuite.From_EntityAsEntityWithSchemaData))]
    public void From_EntityAsEntityWithSchema(SqlTestCase testCase)
    {
        // Arrange
        var db = CreateBuilder();

        // Act
        testCase.Act(() =>
        {
            db.Entity<Product>(out var p, "Product");
            return db.Append($$"""
                SELECT
                    {{p.Id}}
                FROM {{p:base}} AS {{p:alias}}
                """).Build();
        });

        // Assert
        testCase.Assert();
        db.AssertAotIntercepted();
    }

    [SqlTest(nameof(IFromAsTestSuite.FromAsEntityAsItsOwnAliasInceptionData))]
    public void From_As_EntityAsItsOwnAlias_Inception(SqlTestCase testCase)
    {
        // Arrange
        var db = CreateBuilder();

        // Act
        testCase.Act(() =>
        {
            db.Entity<Product>(out var p);
            return db.Append($$"""
                SELECT {{p}}
                FROM {{p:base}} AS {{p:alias}}
                """).Build();
        });

        // Assert
        testCase.Assert();
        db.AssertAotIntercepted();
    }

    [SqlTest(nameof(IFromAsTestSuite.From_EntityAutoAliasingData))]
    public void From_EntityAutoAliasing(SqlTestCase testCase)
    {
        // Arrange
        var db = CreateBuilder();
        db.Context.Options.EntityAutoAliasing = true;

        // Act
        testCase.Act(() =>
        {
            db.Entity<Product>(out var prod);
            return db.Append($$"""
                SELECT
                    {{prod.Id}}
                FROM {{prod}}
                """).Build();
        });

        // Assert
        testCase.Assert();
        db.AssertAotIntercepted();
    }

    [SqlTest(nameof(IFromAsTestSuite.From_EntityAutoAliasingManualOverrideData))]
    public void From_EntityAutoAliasing_ManualOverride(SqlTestCase testCase)
    {
        // Arrange
        var db = CreateBuilder();
        db.Context.Options.EntityAutoAliasing = true;

        // Act
        testCase.Act(() =>
        {
            db.Entity<Product>(out var prod);
            return db.Append($$"""
                SELECT
                    {{prod.Id}}
                FROM {{prod}} AS p
                """).Build();
        });

        // Assert
        testCase.Assert();
        db.AssertAotIntercepted();
    }

    [SqlTest(nameof(IFromAsTestSuite.From_AutoAliasingInceptionData))]
    public void From_AutoAliasing_Inception(SqlTestCase testCase)
    {
        // Arrange
        var db = CreateBuilder();
        db.Context.Options.EntityAutoAliasing = true;

        // Act
        testCase.Act(() =>
        {
            db.Entity<Product>(out var myProd);
            return db.Append($$"""
                SELECT {{myProd}}
                FROM {{myProd}}
                """).Build();
        });

        // Assert
        testCase.Assert();
        db.AssertAotIntercepted();
    }
}