using SqlInterpol.Configuration;
using SqlInterpol.Schema;
using Xunit;

namespace SqlInterpol.Tests.Schema;

public class SqlMetadataBuilderTests
{
    public class Product
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int CategoryId { get; set; }
    }

    public class Category
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    [Fact]
    public void Builder_Populates_ISqlMapping_List_With_Table_And_Schema()
    {
        // Arrange
        var options = new SqlInterpolOptions();

        // Act
        options.Metadata
            .Entity<Product>(out var _)
                .Table("tbl_product")
                .Schema("sales");

        // Assert
        Assert.Single(options.Mappings);
        var mapping = options.Mappings[0];
        
        Assert.Equal("tbl_product", mapping.TableName);
        Assert.Equal("sales", mapping.SchemaName);
        Assert.Empty(mapping.Columns);
    }

    [Fact]
    public void Builder_Captures_Column_Renames_Via_CallerArgumentExpression()
    {
        // Arrange
        var options = new SqlInterpolOptions();

        // Act
        options.Metadata
            .Entity<Product>(out var p)
                .Column(p.Name, c => c.Name("product_name"))
                .Column(p.CategoryId, c => c.Name("cat_id"));

        // Assert
        var mapping = options.Mappings.First();
        var columns = mapping.Columns.ToList();
        
        Assert.Equal(2, columns.Count);
        
        // Verifies that 'p.Name' was correctly extracted as "Name"
        Assert.Contains(columns, c => c.PropertyName == nameof(Product.Name) && c.ColumnName == "product_name");
        
        // Verifies that 'p.CategoryId' was correctly extracted as "CategoryId"
        Assert.Contains(columns, c => c.PropertyName == nameof(Product.CategoryId) && c.ColumnName == "cat_id");
    }

    [Fact]
    public void Builder_Supports_Continuous_Fluent_Chaining_Across_Entities()
    {
        // Arrange
        var options = new SqlInterpolOptions();

        // Act
        options.Metadata
            .Entity<Product>(out var p)
                .Table("tbl_product")
                .Column(p.Name, c => c.Name("product_name"))
            .Entity<Category>(out var c)
                .Table("tbl_category")
                .Column(c.Name, col => col.Name("category_name"));

        // Assert
        Assert.Equal(2, options.Mappings.Count);
        
        var productMapping = options.Mappings[0];
        var categoryMapping = options.Mappings[1];

        Assert.Equal("tbl_product", productMapping.TableName);
        Assert.Equal("product_name", productMapping.Columns.First().ColumnName);
        
        Assert.Equal("tbl_category", categoryMapping.TableName);
        Assert.Equal("category_name", categoryMapping.Columns.First().ColumnName);
    }

    [Fact]
    public void Builder_Sets_EntityTypeOverride_For_Views()
    {
        // Arrange
        var options = new SqlInterpolOptions();

        // Act
        options.Metadata
            .Entity<Product>(out var p)
                .View("vw_products");

        // Assert
        var mapping = options.Mappings.First();
        Assert.Equal("vw_products", mapping.TableName);
        
        // Cast to access internal properties specific to the configuration class
        var internalConfig = Assert.IsType<SqlEntityConfiguration>(mapping);
        Assert.Equal(SqlEntityType.View, internalConfig.EntityTypeOverride);
    }
}