namespace SqlInterpol;

public interface ISqlReference : ISqlFragment
{
    ISqlFragment Source { get; }
    string? Alias { get; set; }
    bool IsAliasQuoted { get; set; }
    string FallbackAlias { get; }
}