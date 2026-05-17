using SqlInterpol.Parsing;

namespace SqlInterpol;

public interface ISqlInterpolationParser
{
    string? ProcessLiteral(ISqlParserContext context, ReadOnlySpan<char> span);
    SqlSegment ProcessValue(ISqlParserContext context, object? value);
    string ReplaceKeyword(string sql, string keyword, string replacement);
}