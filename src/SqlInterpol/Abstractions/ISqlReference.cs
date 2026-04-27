namespace SqlInterpol.Abstractions;

public interface ISqlReference : ISqlFragment
{
    ISqlProjection Parent { get; }

    string? Alias { get; }
}