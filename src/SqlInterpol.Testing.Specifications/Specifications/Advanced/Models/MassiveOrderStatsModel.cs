using SqlInterpol.Schema;

namespace SqlInterpol.Testing.Specifications;

public abstract partial class AdvancedTestSuite
{
    [SqlTable(name: "MassiveOrderStats")]
    public record MassiveOrderStatsModel
    {
        public int OrderId { get; init; }
        public string ProductName { get; init; } = string.Empty;
        public decimal TotalAmount { get; init; }
    }
}