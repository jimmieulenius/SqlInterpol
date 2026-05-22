
namespace SqlInterpol.Parsing;

public interface ISqlParserState
{
    SqlKeyword? CurrentKeyword { get; set; }
    bool IsInsideString { get; set; }
    int ParameterCount { get; set; }
    ISqlFragment? LastAliasableTarget { get; set; }
    bool ExpectsAliasOnly { get; set; }
    SqlSegment? LastSegment { get; set; }
    Dictionary<ISqlEntityBase, SqlEntityRole> EntityRoles { get; }
    ISqlEntityBase? ActiveEntityTarget { get; set; }
    bool InBlockComment { get; set; }
    bool InLineComment { get; set; }
    int ParenDepth { get; set; }

    void Reset();
}