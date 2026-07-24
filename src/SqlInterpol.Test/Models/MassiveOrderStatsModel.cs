
using SqlInterpol.Schema;

namespace SqlInterpol.Test.Models;

[SqlTable(name: "MassiveOrderStats")]
public record MassiveOrderStatsModel
{
    public int OrderId { get; init; }
    public string ProductName { get; init; } = string.Empty;
    public decimal TotalAmount { get; init; }
}