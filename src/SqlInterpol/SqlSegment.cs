using System.Runtime.InteropServices;
using SqlInterpol.Parsing;

namespace SqlInterpol;

[StructLayout(LayoutKind.Auto)]
public readonly struct SqlSegment
{
    public readonly SqlSegmentType Type;
    public readonly object? Value;
    public readonly SqlKeyword? Context;

    public SqlSegment(SqlSegmentType type, object? value, SqlKeyword? context = null)
    {
        Type = type;
        Value = value;
        Context = context;
    }
}