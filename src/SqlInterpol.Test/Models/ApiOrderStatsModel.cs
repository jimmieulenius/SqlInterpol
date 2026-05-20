
namespace SqlInterpol.Test.Models;

[SqlTable("OrderStats")]
public record ApiOrderStatsModel
{
    public int CustomerId { get; init; }
    public int OrderId { get; init; }
    public decimal TotalAmount { get; init; }
}