using SqlInterpol.Config;

namespace SqlInterpol;

public readonly record struct SqlRawFragment(string Value) : ISqlFragment
{
    // The standard implementation just ignores the context
    public string ToSql(SqlContext context) => Value;
}