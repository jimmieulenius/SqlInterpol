using SqlInterpol.Parsing;

namespace SqlInterpol;

public interface ISqlInterpolationParser
{
    void ProcessLiteral(ISqlParserContext context, ReadOnlySpan<char> span);
    SqlSegment ProcessValue(ISqlParserContext context, object? value);
}