using SqlInterpol.Config;

namespace SqlInterpol;

public readonly record struct SqlRawFragment(string Value) : ISqlFragment
{
    private readonly Func<SqlContext, string>? _renderer;

    public SqlRawFragment(Func<SqlContext, string> renderer) : this(string.Empty)
    {
        _renderer = renderer;
    }

    public string ToSql(SqlContext context) => _renderer != null ? _renderer(context) : Value;
}