
namespace SqlInterpol.Benchmarks.Models;

[SqlTable("Products", schema: "dbo")]
public class Product
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public int CategoryId { get; set; }
    public decimal Price { get; set; }
    public bool IsActive { get; set; }
}

[SqlTable("Orders", schema: "dbo")]
public class Order
{
    public int Id { get; set; }
    public int CustomerId { get; set; }
    public decimal Total { get; set; }
    public DateTime CreatedAt { get; set; }
}

[SqlTable("OrderLines", schema: "dbo")]
public class OrderLine
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    public int ProductId { get; set; }
    public decimal Price { get; set; }
    public int Quantity { get; set; }
}
