namespace SqlInterpol;

public class SqlSegment(SqlSegmentType type, object? value, bool isAliasTarget = false)
{
    public SqlSegmentType Type { get; } = type;
    public object? Value { get; } = value;
    public bool IsAliasTarget { get; } = isAliasTarget;
}