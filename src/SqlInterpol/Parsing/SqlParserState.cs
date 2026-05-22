namespace SqlInterpol.Parsing;

/// <summary>
/// The default mutable implementation of <see cref="ISqlParserState"/> used during interpolated SQL parsing.
/// </summary>
internal class SqlParserState : ISqlParserState
{
    public SqlKeyword? CurrentKeyword { get; set; }
    public bool IsInsideString { get; set; }
    public int ParameterCount { get; set; }
    public ISqlFragment? LastAliasableTarget { get; set; }
    public bool ExpectsAliasOnly { get; set; }
    public SqlSegment? LastSegment { get; set; }
    public Dictionary<ISqlEntityBase, SqlEntityRole> EntityRoles { get; private set; } = [];
    public ISqlEntityBase? ActiveEntityTarget { get; set; }
    public bool InBlockComment { get; set; }
    public bool InLineComment { get; set; }
    public int ParenDepth { get; set; }
    private Dictionary<string, object?>? _properties;
    public IDictionary<string, object?> Properties => _properties ??= [];

    /// <inheritdoc />
    public void Reset()
    {
        _properties = null;
        CurrentKeyword = null;
        IsInsideString = false;
        ParameterCount = 0;
        LastAliasableTarget = null;
        ExpectsAliasOnly = false;
        LastSegment = null;
        EntityRoles = [];
        ActiveEntityTarget = null;
        InBlockComment = false;
        InLineComment = false;
        ParenDepth = 0;
    }
}