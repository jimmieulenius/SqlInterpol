using SqlInterpol.Parsing;

namespace SqlInterpol.Config;

public interface ISqlParser
{
    void ProcessLiteral(SqlContext context, ReadOnlySpan<char> span);
    SqlSegment ProcessValue(SqlContext context, object? value);
}