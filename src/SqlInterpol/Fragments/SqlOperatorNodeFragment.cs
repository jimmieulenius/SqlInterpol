namespace SqlInterpol;

internal sealed class SqlOperatorNode : ISqlFragment
{
    private readonly string _operator;
    private readonly object _left;
    private readonly object _right;

    public SqlOperatorNode(string op, object left, object right)
    {
        _operator = op;
        _left = left;
        _right = right;
    }

    public string ToSql(ISqlContext context, SqlRenderMode mode = SqlRenderMode.Default)
    {
        var leftStr = _left is ISqlFragment lf ? lf.ToSql(context) : _left?.ToString() ?? "NULL";
        var rightStr = _right is ISqlFragment rf ? rf.ToSql(context) : _right?.ToString() ?? "NULL";
        return $"{leftStr} {_operator} {rightStr}";
    }
}