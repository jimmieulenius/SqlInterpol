namespace SqlInterpol;

internal sealed class SqlFunctionNodeFragment(string functionName, object[] args) : ISqlFragment
{
    private readonly string _functionName = functionName;
    private readonly object[] _args = args;

    public string ToSql(ISqlContext context, SqlRenderMode mode = SqlRenderMode.Default)
    {
        // Recursively render fragments or default to strings
        var formattedArgs = _args.Select(a => a is ISqlFragment f ? f.ToSql(context) : a?.ToString() ?? "NULL");
        return $"{_functionName}({string.Join(", ", formattedArgs)})";
    }
}