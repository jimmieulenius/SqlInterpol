using System.Runtime.InteropServices;
using SqlInterpol.Parsing;

namespace SqlInterpol;

[StructLayout(LayoutKind.Auto)]
public readonly struct SqlSegment
{
    public readonly SqlSegmentType Type;
    public readonly object? Value;
    public readonly SqlKeyword? Keyword;

    public SqlSegment(SqlSegmentType type, object? value, SqlKeyword? keyword = null)
    {
        Type = type;
        Value = value;
        Keyword = keyword;
    }
}