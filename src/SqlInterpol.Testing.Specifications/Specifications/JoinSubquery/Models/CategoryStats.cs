using SqlInterpol.Schema;

namespace SqlInterpol.Testing.Specifications;

public abstract partial class JoinSubqueryTestSuite
{
    [SqlTable(schema: "dbo")]
    public record CategoryStats
    {
        public int CategoryId { get; init; }
        public decimal TotalPrice { get; init; }
    }
}