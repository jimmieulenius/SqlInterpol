using SqlInterpol.Configuration;
using SqlInterpol.Testing.Xunit;

namespace SqlInterpol.Testing.Specifications;

[SqlTestSuite(typeof(IInsertTestSuite))]
public abstract partial class InsertTestSuite
{
    [SqlGeneratorIgnore]
    public abstract SqlBuilder CreateBuilder(SqlInterpolOptions? options = null);

    [SqlTest(nameof(IInsertTestSuite.InsertData))]
    public void Insert_WithImplicitSyntax(SqlTestCase testCase)
    {
        // Arrange
        var db = CreateBuilder();
        var newProduct = new { Name = "Test Product", CategoryId = 5, Price = 19.99m };

        // Act
        testCase.Act(() => 
        {
            db.Entity<Product>(out var p);
            return db.Append($$"""
            INSERT INTO {{p}}
            {{newProduct}}
            """).Build();
        });

        // Assert
        testCase.Assert();
        db.AssertAotIntercepted();
    }

    [SqlTest(nameof(IInsertTestSuite.InsertData))]
    public void Insert_WithExplicitValuesKeyword(SqlTestCase testCase)
    {
        // Arrange
        var db = CreateBuilder();
        var newProduct = new { Name = "Test Product", CategoryId = 5, Price = 19.99m };

        // Act
        testCase.Act(() => 
        {
            db.Entity<Product>(out var p);
            return db.Append($$"""
            INSERT INTO {{p}}
            VALUES {{newProduct}}
            """).Build();
        });

        // Assert
        testCase.Assert();
        db.AssertAotIntercepted();
    }

    [SqlTest(nameof(IInsertTestSuite.ManualInsertData))]
    public void Insert_PureManual(SqlTestCase testCase)
    {
        // Arrange
        var db = CreateBuilder();
        var status = "Manual";
        var total = 100.00m;
        
        // Act
        testCase.Act(() => 
        {
            db.Entity<OrderModel>(out var o);
            return db.Append($$"""
            INSERT INTO {{o}}
            ({{o.Status}}, {{o.Total}})
            VALUES ({{status}}, {{total}})
            """).Build();
        });

        // Assert
        testCase.Assert();
        db.AssertAotIntercepted();
    }

    [SqlTest(nameof(IInsertTestSuite.BulkInsertData))]
    public void Insert_BulkImplicitSyntax(SqlTestCase testCase)
    {
        // Arrange
        var db = CreateBuilder();
        var products = new[]
        {
            new { Name = "Prod1", CategoryId = 1, Price = 10m },
            new { Name = "Prod2", CategoryId = 2, Price = 20m }
        };

        // Act
        testCase.Act(() => 
        {
            db.Entity<Product>(out var p);
            return db.Append($$"""
            INSERT INTO {{p}} VALUES {{products}}
            """).Build();
        });

        // Assert
        testCase.Assert();
        db.AssertAotIntercepted();
    }

    [SqlTest(nameof(IInsertTestSuite.ReturningSingleData))]
    public void Insert_ReturningSingleColumn(SqlTestCase testCase)
    {
        // Arrange
        var db = CreateBuilder();
        var newProduct = new { Name = "Test Product", CategoryId = 5, Price = 19.99m };

        // Act
        testCase.Act(() => 
        {
            db.Entity<Product>(out var p);

#pragma warning disable SQLIG10 // RETURNING clause forces JIT fallback
            return db.Append($$"""
            INSERT INTO {{p}} {{newProduct}}
            RETURNING {{p.Id}}
            """).Build();
#pragma warning restore SQLIG10
        });

        // Assert
        testCase.Assert();
        // Omitted db.AssertAotIntercepted() because RETURNING forces JIT execution
    }

    [SqlTest(nameof(IInsertTestSuite.ReturningMultipleData))]
    public void Insert_ReturningMultipleColumns(SqlTestCase testCase)
    {
        // Arrange
        var db = CreateBuilder();
        var newProduct = new { Name = "Test Product", CategoryId = 5, Price = 19.99m };

        // Act
        testCase.Act(() => 
        {
            db.Entity<Product>(out var p);

#pragma warning disable SQLIG10 // RETURNING clause forces JIT fallback
            return db.Append($$"""
            INSERT INTO {{p}} {{newProduct}}
            RETURNING {{p.Id}}, {{p.Name}}
            """).Build();
#pragma warning restore SQLIG10
        });

        // Assert
        testCase.Assert();
        // Omitted db.AssertAotIntercepted() because RETURNING forces JIT execution
    }

    [SqlTest(nameof(IInsertTestSuite.InsertWithIgnoreData))]
    public void Insert_WithIgnoredProperty(SqlTestCase testCase)
    {
        // Arrange
        var db = CreateBuilder();
        var product = new ProductWithIgnoreModel { Id = 1, Name = "Gadget", RuntimeCacheToken = "OmitThisColumn" };

        // Act
        testCase.Act(() => 
        {
            db.Entity<ProductWithIgnoreModel>(out var p);
            return db.Append($$"""
            INSERT INTO {{p}} {{product}}
            """).Build();
        });

        // Assert
        testCase.Assert();
        db.AssertAotIntercepted();
    }

    [SqlTest(nameof(IInsertTestSuite.InsertComplexData))]
    public void Insert_RespectsEnumFormats_AndIgnoresComplexTypes(SqlTestCase testCase)
    {
        // Arrange
        var db = CreateBuilder();
        db.Context.Options.EnumFormat = SqlEnumFormat.Integer;

        var product = new ComplexProduct
        {
            Id = 42,
            Name = "Mechanical Keyboard",
            Status = ProductStatus.Available,
            Category = ProductCategoryType.Electronics,
            Supplier = new Supplier { Id = 99, Name = "TechCorp" }
        };

        // Act
        testCase.Act(() => 
        {
            db.Entity<ComplexProduct>(out var p);
            return db.Append($$"""
            INSERT INTO {{p}}
            VALUES {{product}};
            """).Build();
        });

        // Assert
        testCase.Assert();
        db.AssertAotIntercepted();
    }
}