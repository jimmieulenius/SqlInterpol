
namespace SqlInterpol.Test.Models;

[SqlTable(Schema = "dbo")]
public record CategoryStats
{
    public int CategoryId { get; init; }
    public decimal TotalPrice { get; init; }
}