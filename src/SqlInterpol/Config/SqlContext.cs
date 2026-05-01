using SqlInterpol.Parsing;

namespace SqlInterpol.Config;

public class SqlContext(SqlBuilder builder, ISqlDialect dialect, SqlInterpolOptions? options = null)
{
    public SqlBuilder Builder { get; } = builder;
    public ISqlDialect Dialect { get; } = dialect;
    public SqlInterpolOptions Options { get; } = options ?? new() { Dialect = dialect.Kind };
    public Dictionary<string, object?> Parameters { get; } = [];
    internal SqlParseState State = new();
}