using SqlInterpol.Metadata;

namespace SqlInterpol.Test.Models;

[SqlTable(name: "Products", schema: "dbo")]
public class Product
{
    public int Id { get; set; }
    [SqlColumn("PROD_NAME")]
    public string Name { get; set; } = null!;
    public bool IsActive { get; set; }
    public int CategoryId { get; set; }
}