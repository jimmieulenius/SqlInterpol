using System.Linq;
using SqlInterpol.Configuration;
using SqlInterpol.Schema;
using Xunit;

namespace SqlInterpol.Tests.Schema;

public class SqlMetadataPrecedenceTests
{
    [SqlTable("LegacyCustomer", "old_schema")]
    public class Customer
    {
        [SqlColumn("LegacyId")]
        public int Id { get; set; }
        
        public string Email { get; set; } = string.Empty;
    }

    [Fact]
    public void Registry_Defaults_To_Attributes_If_No_Fluent_Config_Present()
    {
        // Arrange
        var options = new SqlInterpolOptions(); // Empty fluent configuration

        // Act
        var meta = SqlMetadataRegistry.GetMetadata<Customer>(options);

        // Assert
        Assert.Equal("LegacyCustomer", meta.Name);
        Assert.Equal("old_schema", meta.Schema);
        
        var idProp = meta.Columns.Keys.First(k => k.Name == nameof(Customer.Id));
        Assert.Equal("LegacyId", meta.Columns[idProp]);
    }

    [Fact]
    public void Registry_Fluent_API_Overrides_Attributes()
    {
        // Arrange
        var options = new SqlInterpolOptions();
        
        options.Metadata
            .Entity<Customer>(out var c)
                .Table("ModernCustomer")
                .Schema("new_schema")
                .Column(c.Id, col => col.Name("ModernId")) // Using lambda builder
                .Column(c.Email, "ModernEmail");           // Using string shorthand

        // Act
        var meta = SqlMetadataRegistry.GetMetadata<Customer>(options);

        // Assert
        Assert.Equal("ModernCustomer", meta.Name); // Overridden via Fluent API
        Assert.Equal("new_schema", meta.Schema);   // Overridden via Fluent API
        
        var idProp = meta.Columns.Keys.First(k => k.Name == nameof(Customer.Id));
        var emailProp = meta.Columns.Keys.First(k => k.Name == nameof(Customer.Email));
        
        Assert.Equal("ModernId", meta.Columns[idProp]);       // Overridden via lambda
        Assert.Equal("ModernEmail", meta.Columns[emailProp]); // Overridden via shorthand
    }
    
    [Fact]
    public void Registry_Fluent_API_Partial_Override_Leaves_Other_Attributes_Intact()
    {
        // Arrange
        var options = new SqlInterpolOptions();
        
        // We only override the column, leaving the Table and Schema to fall back to attributes
        options.Metadata
            .Entity<Customer>(out var c)
                .Column(c.Id, "ModernId"); // Using string shorthand

        // Act
        var meta = SqlMetadataRegistry.GetMetadata<Customer>(options);

        // Assert
        Assert.Equal("LegacyCustomer", meta.Name); // Falls back to attribute
        Assert.Equal("old_schema", meta.Schema);   // Falls back to attribute
        
        var idProp = meta.Columns.Keys.First(k => k.Name == nameof(Customer.Id));
        Assert.Equal("ModernId", meta.Columns[idProp]); // Overridden via Fluent API
    }
}