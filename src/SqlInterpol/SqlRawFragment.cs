using SqlInterpol.Config;

namespace SqlInterpol;

public readonly record struct SqlRawFragment(string Value) : ISqlFragment
{
    public string ToSql(SqlContext context, SqlRenderMode mode = SqlRenderMode.Default) => Value;
}