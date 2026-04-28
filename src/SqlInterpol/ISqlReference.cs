namespace SqlInterpol;

public interface ISqlReference : ISqlFragment
{
    ISqlProjection Source { get; }

    string? Alias { get; set; }
}