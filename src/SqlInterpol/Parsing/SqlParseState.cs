namespace SqlInterpol.Parsing;

internal class SqlParseState
{
    public SqlKeyword? CurrentKeyword;
    public bool IsInsideString;
    public int ParameterCount;
    public ISqlProjection? PendingAliasCapture { get; set; }
    public bool ExpectsAliasOnly { get; set; }
    public ISqlProjection? LastAliasableTarget { get; set; }
    public SqlSegment? LastSegment;
}