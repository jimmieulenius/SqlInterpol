using System;
using SqlInterpol.Parsing;
using SqlInterpol.Test.Models;
using Xunit;

namespace SqlInterpol.Test;

public class MetadataTests
{
    [Fact]
    public void GetColumnName_ThrowsArgumentException_WhenExpressionIsInvalid()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => 
            SqlMetadataRegistry.GetColumnName<Product>(x => x.Id.ToString())
        );

        Assert.Contains("is not a valid property selector", exception.Message);
    }

    [Fact]
    public void GetEntityName_ThrowsArgumentNullException_WhenEntityIsNull()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => 
            SqlMetadataRegistry.GetEntityName(null!)
        );
    }

    [Fact]
    public void GetEntityName_ThrowsArgumentException_WhenEntityMissingGenericInterface()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => 
            SqlMetadataRegistry.GetEntityName(new InvalidDummyEntity())
        );

        Assert.Contains("must implement ISqlEntityBase<T>", exception.Message);
    }

    [Fact]
    public void GetColumnName_ThrowsArgumentException_WhenPropertyIsIgnored()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => 
            SqlMetadataRegistry.GetColumnName<MetadataErrorModel>(x => x.UnmappedProperty)
        );

        Assert.Equal("Property 'UnmappedProperty' not found on 'MetadataErrorModel'.", exception.Message);
    }

    private class InvalidDummyEntity : ISqlEntityBase 
    {
        public ISqlReference this[string columnName] => throw new NotImplementedException();
        public ISqlReference Reference => throw new NotImplementedException();
        public ISqlDeclaration Declaration => throw new NotImplementedException();
        public SqlEntityRole Role => throw new NotImplementedException();
        public Type ModelType => throw new NotImplementedException();

        public ISqlFragment Column(string name) => throw new NotImplementedException();
        public string ToSql(ISqlContext context, SqlRenderMode mode = SqlRenderMode.Default) => throw new NotImplementedException();
    }

    private class MetadataErrorModel
    {
        public int Id { get; set; }
        
        [SqlIgnore]
        public string UnmappedProperty { get; set; } = "";
    }
}