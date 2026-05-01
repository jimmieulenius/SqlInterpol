namespace SqlInterpol;

public record SqlSegment
{
    public SqlSegmentType Type { get;}
    public object? Value { get; }
    public SqlKeyword? Keyword { get; }
    public SqlRenderMode RenderMode { get; set; }

    public SqlSegment(SqlSegmentType type, object? value, SqlKeyword? keyword = null, SqlRenderMode mode = SqlRenderMode.Default)
    {
        Type = type;
        Value = value;
        Keyword = keyword;
        RenderMode = mode;
    }
}