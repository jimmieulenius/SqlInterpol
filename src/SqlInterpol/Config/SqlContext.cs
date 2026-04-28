namespace SqlInterpol.Config;

public class SqlContext(ISqlDialect dialect, SqlInterpolOptions? options = null)
{
    public ISqlDialect Dialect { get; } = dialect;
    public SqlInterpolOptions Options { get; } = options ?? new() { Dialect = dialect.Kind };
    public Dictionary<string, object?> Parameters { get; } = [];
    public ISqlProjection? PendingAliasCapture { get; set; }
}