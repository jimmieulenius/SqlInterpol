
namespace SqlInterpol;

public readonly record struct SqlRawFragment(string Value) : ISqlFragment
{
    public string ToSql(ISqlContext context, SqlRenderMode mode = SqlRenderMode.Default) => Value;
}