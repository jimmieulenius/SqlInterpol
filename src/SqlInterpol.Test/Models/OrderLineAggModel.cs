using SqlInterpol.Schema;

namespace SqlInterpol.Test.Models;

[SqlTable("OrderLineAgg")]
public record OrderLineAggModel
{
    public int OrderId { get; init; }
    public int ProductId { get; init; }
    public decimal TotalAmount { get; init; }
}