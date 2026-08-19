using SqlInterpol.Schema;

namespace SqlInterpol.Testing.Specifications;

public abstract partial class CteTestSuite
{
    [SqlTable(schema: "dbo")]
    public record CategoryStats
    {
        public int CategoryId { get; init; }
        public decimal TotalPrice { get; init; }
    }
}