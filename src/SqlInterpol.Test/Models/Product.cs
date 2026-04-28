using SqlInterpol.Metadata;

namespace SqlInterpol.Test.Models;

[SqlTable(Name = "Products", Schema = "dbo")]
public class Product
{
    public int Id { get; set; }
    [SqlColumn("Name")] 
    public string? ProductName { get; set; }
    public bool IsActive { get; set; }
}