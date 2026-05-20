
namespace SqlInterpol.Parsing;

public interface ISqlParserContext : ISqlContext
{
    SqlBuilder Builder { get; }
    ISqlParserState ParserState { get; }
}