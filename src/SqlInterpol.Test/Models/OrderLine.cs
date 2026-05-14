namespace SqlInterpol.Test.Models;

public class OrderLine
{
    public int OrderId { get; set; }

    public int ProductItemNumber { get; set; }

    public int Quantity { get; set; }

    public decimal Price { get; set; }

    public int ProductId { get; set; }
}