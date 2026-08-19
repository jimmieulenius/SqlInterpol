using SqlInterpol.Schema;

namespace SqlInterpol.Testing.Specifications;

public abstract partial class GroupBySubqueryTestSuite
{
    [SqlTable("Stats")]
    public record StatsModel(int CategoryId, decimal MaxPrice);
}