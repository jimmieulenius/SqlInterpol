using System.Runtime.CompilerServices;
using SqlInterpol.Config;

namespace SqlInterpol.Parsing;

public static class SqlParser
{
    public static ISqlParser Instance { get; internal set; } = new DefaultSqlParser();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ProcessLiteral(SqlContext context, ReadOnlySpan<char> span)
    {
        Instance.ProcessLiteral(context, span);
    }
}