

using SqlInterpol.Metadata;

namespace SqlInterpol.Test.Models;

[SqlTable(schema: "dbo")]
public class Product
{
    /// <summary>Product item number (SKU)</summary>
    // [SqlColumn]
    public int ItemNumber { get; set; }

    /// <summary>Product name - mapped from database column 'ProductName'</summary>
    // [SqlColumn("ProductName")]
    public string Name { get; set; } = null!;

    /// <summary>Product price</summary>
    // [SqlColumn]
    public decimal Price { get; set; }
}