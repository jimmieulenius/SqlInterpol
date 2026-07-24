using SqlInterpol.Schema;

namespace SqlInterpol.Test.Models;

[SqlTable(name: "OrderStats")]
public record ApiOrderStatsModel
{
    public int CustomerId { get; init; }
    public int OrderId { get; init; }
    public decimal TotalAmount { get; init; }
}