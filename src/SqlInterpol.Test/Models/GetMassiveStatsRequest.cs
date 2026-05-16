namespace SqlInterpol.Test.Models;

public record GetMassiveStatsRequest
{
    public string? ProductNameFilter { get; init; }
    public List<string> SelectFields { get; init; } = new();
    public List<SortCriteria> SortFields { get; init; } = new();
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;
}