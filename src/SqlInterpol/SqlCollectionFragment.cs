using SqlInterpol.Config;

namespace SqlInterpol;

public sealed class SqlCollectionFragment(List<string> parameterKeys) : ISqlFragment
{
    public string ToSql(ISqlContext context, SqlRenderMode mode = SqlRenderMode.Default)
    {
        if (parameterKeys.Count == 0)
            return string.Empty;

        return string.Join(", ", parameterKeys);
    }
}