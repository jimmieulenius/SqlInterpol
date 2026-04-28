using SqlInterpol.Metadata;

namespace SqlInterpol.Test.Models;

[SqlTable(schema: "dbo")]
public class Product
{
    public int ItemNumber { get; set; }

    public string Name { get; set; } = null!;

    public decimal Price { get; set; }
}