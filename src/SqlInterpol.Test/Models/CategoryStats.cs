using SqlInterpol.Schema;

namespace SqlInterpol.Test.Models;

[SqlTable(schema: "dbo")]
public record CategoryStats
{
    public int CategoryId { get; init; }
    public decimal TotalPrice { get; init; }
}