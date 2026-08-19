using SqlInterpol.Schema;

namespace SqlInterpol.Testing.Specifications;

public abstract partial class OrderBySubqueryTestSuite
{
    [SqlTable("Stats")]
    public record StatsModel(int CategoryId, decimal MaxPrice);
}