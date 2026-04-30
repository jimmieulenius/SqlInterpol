namespace SqlInterpol;

public interface ISqlReference : ISqlFragment
{
    ISqlEntity Source { get; }

    string? Alias { get; set; }
}