using SqlInterpol.Schema;

namespace SqlInterpol.Test.Models;

[SqlTable(name: "Orders", schema: "dbo")]
public record OrderModel
{
    public int Id { get; init; }
    public int CustomerId { get; init; }
    [SqlColumn("order_status")]
    public string Status { get; init; } = string.Empty;
    public decimal Total { get; init; }
    public int CategoryId { get; init; }
    [SqlColumn("created_at")]
    public DateTime CreatedAt { get; init; }
}