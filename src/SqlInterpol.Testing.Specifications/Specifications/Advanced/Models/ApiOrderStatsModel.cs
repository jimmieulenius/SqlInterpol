using SqlInterpol.Schema;

namespace SqlInterpol.Testing.Specifications;

public abstract partial class AdvancedTestSuite
{
    [SqlTable(name: "OrderStats")]
    public record ApiOrderStatsModel
    {
        public int CustomerId { get; init; }
        public int OrderId { get; init; }
        public decimal TotalAmount { get; init; }
    }
}