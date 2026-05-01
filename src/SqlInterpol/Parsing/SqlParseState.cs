namespace SqlInterpol.Parsing;

internal class SqlParseState
{
    public SqlKeyword? CurrentKeyword { get; set; }
    public bool IsInsideString { get; set; }
    public int ParameterCount { get; set; }
    public ISqlProjection? LastAliasableTarget { get; set; }
    public bool ExpectsAliasOnly { get; set; }
}