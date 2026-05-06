using SqlInterpol.Config;

namespace SqlInterpol.Parsing;

public interface ISqlParserState
{
    SqlKeyword? CurrentKeyword { get; set; }
    bool IsInsideString { get; set; }
    int ParameterCount { get; set; }
    bool ExpectsAliasOnly { get; set; }
    ISqlProjection? LastAliasableTarget { get; set; }
    SqlSegment? LastSegment { get; set; }
}