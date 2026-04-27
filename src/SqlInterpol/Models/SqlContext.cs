using SqlInterpol.Abstractions;
using SqlInterpol.Services;

namespace SqlInterpol.Models;

public readonly struct SqlContext(SqlInterpolOptions options, ISqlDialectService dialect)
{
    public SqlInterpolOptions Options { get; } = options;
    public ISqlDialectService Dialect { get; } = dialect ?? new SqlServerDialectService();
}