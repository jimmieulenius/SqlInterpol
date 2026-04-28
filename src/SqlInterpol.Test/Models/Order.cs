using SqlInterpol.Metadata;

namespace SqlInterpol.Test.Models;

[SqlTable("Orders", "dbo")]
public class Order
{
    [SqlColumn]
    public int OrderId { get; set; }

    [SqlColumn]
    public int ProductItemNumber { get; set; }

    [SqlColumn]
    public DateTime OrderDate { get; set; }

    [SqlColumn]
    public int Quantity { get; set; }
}