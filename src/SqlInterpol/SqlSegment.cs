using SqlInterpol.Parsing;

namespace SqlInterpol;

public readonly struct SqlSegment
{
    public readonly SqlSegmentType Type;
    public readonly object? Value;
    public readonly SqlKeyword? Keyword;
    public readonly SqlRenderMode RenderMode;

    public SqlSegment(SqlSegmentType type, object? value, SqlKeyword? keyword = null, SqlRenderMode mode = SqlRenderMode.Default)
    {
        Type = type;
        Value = value;
        Keyword = keyword;
        RenderMode = mode;
    }
}