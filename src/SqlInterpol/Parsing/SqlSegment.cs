using System.Runtime.InteropServices;

namespace SqlInterpol.Parsing;

[StructLayout(LayoutKind.Auto)]
public readonly struct SqlSegment
{
    public readonly SegmentType Type;
    public readonly object? Value;
    public readonly SqlKeyword? Context;

    public SqlSegment(SegmentType type, object? value, SqlKeyword? context = null)
    {
        Type = type;
        Value = value;
        Context = context;
    }
}