namespace SqlInterpol.Config;

public interface ISqlParser
{
    void ProcessLiteral(SqlContext context, ReadOnlySpan<char> span);
}