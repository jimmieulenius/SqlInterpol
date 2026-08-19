using SqlInterpol.Schema;

namespace SqlInterpol.Testing.Specifications;

public abstract partial class AdvancedTestSuite
{
    [SqlTable("OrderLineAgg")]
    public record OrderLineAggModel
    {
        public int OrderId { get; init; }
        public int ProductId { get; init; }
        public decimal TotalAmount { get; init; }
    }
}