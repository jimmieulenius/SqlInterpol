using SqlInterpol.Attributes;

namespace SqlInterpol.Test.Models;

[SqlTable("Orders", "dbo")]
public class Order
{
    /// <summary>Order ID (PK)</summary>
    [SqlColumn]
    public int OrderId { get; set; }

    /// <summary>Product item number (FK)</summary>
    [SqlColumn]
    public int ProductItemNumber { get; set; }

    /// <summary>Order date</summary>
    [SqlColumn]
    public DateTime OrderDate { get; set; }

    /// <summary>Order quantity</summary>
    [SqlColumn]
    public int Quantity { get; set; }
}