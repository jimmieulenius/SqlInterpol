namespace SqlInterpol.Testing.Specifications;

public abstract partial class AdvancedTestSuite
{
    public record GetOrderStatsRequest
    {
        public int? CustomerId { get; init; }
        public List<string> SelectFields { get; init; } = new();
        public List<SortCriteria> SortFields { get; init; } = new();
        public int Page { get; init; } = 1;
        public int PageSize { get; init; } = 20;
    }
}