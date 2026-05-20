
namespace SqlInterpol.Test.Models;

[SqlTable("MassiveOrderStats")]
public record MassiveOrderStatsModel
{
    public int OrderId { get; init; }
    public string ProductName { get; init; } = string.Empty;
    public decimal TotalAmount { get; init; }
}