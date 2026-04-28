using SqlInterpol.Config;

namespace SqlInterpol.Parsing;

public class LazySqlFragment(ISqlFragment inner, SqlContext context) : ISqlFragment
{
    private readonly ISqlFragment _inner = inner;
    private readonly SqlContext _context = context;

    // This is called by the Builder/Handler only at the end
    public override string ToString() => _inner.ToSql(_context);

    // satisfy the interface
    public string ToSql(SqlContext context) => _inner.ToSql(context);
}