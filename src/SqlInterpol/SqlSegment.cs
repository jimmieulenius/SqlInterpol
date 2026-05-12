namespace SqlInterpol;

public class SqlSegment(SqlSegmentType type, object? value, SqlRenderMode? renderMode = null)
{
    public SqlSegmentType Type { get; } = type;
    public object? Value { get; } = value;
    public SqlRenderMode? RenderMode { get; } = renderMode;
}