namespace SqlInterpol.Config;

public class SqlContext
{
    public ISqlDialect Dialect { get; }
    public SqlInterpolOptions Options { get; }
    public Dictionary<string, object?> Parameters { get; } = new();

    // The context is now a passive recipient of the dialect
    public SqlContext(ISqlDialect dialect, SqlInterpolOptions options)
    {
        Dialect = dialect;
        Options = options;
    }
}