using SqlInterpol.Metadata;

namespace SqlInterpol.Test.Models;

[SqlTable("OrderStats")]
public record OrderStatsModel
{
    public int CategoryId { get; init; }
    
    [SqlColumn("max_price")]
    public decimal MaxPrice { get; init; }
}