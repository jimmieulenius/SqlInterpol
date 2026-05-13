namespace SqlInterpol.Parsing;

internal class SqlParserState : ISqlParserState
{
    public SqlKeyword? CurrentKeyword { get; set; }
    public bool IsInsideString { get; set; }
    public int ParameterCount { get; set; }
    public ISqlFragment? LastAliasableTarget { get; set; }
    public bool ExpectsAliasOnly { get; set; }
    public SqlSegment? LastSegment { get; set; }
    public Dictionary<ISqlEntityBase, SqlEntityRole> EntityRoles { get; } = [];
    public ISqlEntityBase? ActiveEntityTarget { get; set; }
}