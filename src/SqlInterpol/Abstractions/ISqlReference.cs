namespace SqlInterpol.Abstractions;

public interface ISqlReference : ISqlFragment
{
    ISqlProjection Source { get; }

    string? Alias { get; }
}