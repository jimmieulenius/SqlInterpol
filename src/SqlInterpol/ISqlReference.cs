namespace SqlInterpol;

public interface ISqlReference : ISqlFragment
{
    ISqlFragment Source { get; }

    string? Alias { get; set; }
}