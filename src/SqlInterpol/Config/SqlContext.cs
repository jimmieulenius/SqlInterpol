using SqlInterpol.Dialects;

namespace SqlInterpol.Config;

public readonly struct SqlContext(SqlInterpolOptions options, ISqlDialect? dialect = null)
{
    public SqlInterpolOptions Options { get; } = options ?? new SqlInterpolOptions()
    {
        IndentSize = 2
    };
    public ISqlDialect Dialect { get; } = dialect ?? new SqlServerSqlDialect();
    public Dictionary<string, object?> Parameters { get; } = [];
}