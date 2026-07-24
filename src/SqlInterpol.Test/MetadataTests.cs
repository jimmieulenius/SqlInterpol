using SqlInterpol.Schema;

namespace SqlInterpol.Test;

public class MetadataTests
{
    [Fact]
    public void GetPropertyName_ThrowsArgumentException_WhenExpressionIsInvalid()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => 
            SqlExpressionHelper.GetPropertyName<DefaultEntity>(x => x.Id.ToString())
        );

        Assert.Equal("Expression 'x => x.Id.ToString()' must be a direct property access.", exception.Message);
    }

    [Fact]
    public void GetMetadata_ThrowsArgumentNullException_WhenTypeIsNull()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => 
            SqlMetadataRegistry.GetMetadata(null!)
        );
    }

    [Fact]
    public void GetMetadata_OmitsIgnoredProperties()
    {
        // Act
        var meta = SqlMetadataRegistry.GetMetadata<MetadataErrorModel>();

        // Assert
        bool containsIgnoredProperty = meta.Columns.Keys.Any(k => k.Name == nameof(MetadataErrorModel.UnmappedProperty));
        Assert.False(containsIgnoredProperty, "Metadata should not include properties marked with [SqlIgnore].");
    }

    [Fact]
    public void GetMetadata_ResolvesDefaultEntityName()
    {
        // Act
        var meta = SqlMetadataRegistry.GetMetadata<DefaultEntity>();

        // Assert
        Assert.Equal(nameof(DefaultEntity), meta.Name);
        Assert.Null(meta.Schema);
    }

    [Fact]
    public void GetMetadata_ResolvesDefaultColumnName()
    {
        // Act
        var meta = SqlMetadataRegistry.GetMetadata<DefaultEntity>();

        // Assert
        var idColumnMember = meta.Columns.Keys.FirstOrDefault(k => k.Name == nameof(DefaultEntity.Id));
        Assert.NotNull(idColumnMember);
        Assert.Equal("Id", meta.Columns[idColumnMember]);
    }

    [Fact]
    public void GetMetadata_ResolvesCustomEntityName()
    {
        // Act
        var meta = SqlMetadataRegistry.GetMetadata<CustomMappedEntity>();

        // Assert
        Assert.Equal("tbl_custom_entity", meta.Name);
        Assert.Equal("dbo", meta.Schema);
    }

    [Fact]
    public void GetMetadata_ResolvesCustomColumnName()
    {
        // Act
        var meta = SqlMetadataRegistry.GetMetadata<CustomMappedEntity>();

        // Assert
        var idColumnMember = meta.Columns.Keys.FirstOrDefault(k => k.Name == nameof(CustomMappedEntity.Id));
        Assert.NotNull(idColumnMember);
        Assert.Equal("col_custom_id", meta.Columns[idColumnMember]);
    }

    // --- Test Models ---

    private class DefaultEntity
    {
        public int Id { get; set; }
    }

    [SqlTable("tbl_custom_entity", "dbo")]
    private class CustomMappedEntity
    {
        [SqlColumn("col_custom_id")]
        public int Id { get; set; }
    }

    private class MetadataErrorModel
    {
        public int Id { get; set; }
        
        [SqlIgnore]
        public string UnmappedProperty { get; set; } = "";
    }
}