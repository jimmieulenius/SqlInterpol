using SqlInterpol.Abstractions;
using SqlInterpol.Services;

namespace SqlInterpol.Models;

public readonly struct SqlContext(SqlInterpolOptions options, ISqlDialectService dialect)
{
    public SqlInterpolOptions Options { get; } = options ?? new SqlInterpolOptions()
    {
        IndentSize = 2
    };
    public ISqlDialectService Dialect { get; } = dialect ?? new SqlServerDialectService();
    public Dictionary<string, object?> Parameters { get; } = [];
}