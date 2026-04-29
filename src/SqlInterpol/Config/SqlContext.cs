using SqlInterpol.Parsing;

namespace SqlInterpol.Config;

public class SqlContext(ISqlDialect dialect, SqlInterpolOptions? options = null)
{
    public ISqlDialect Dialect { get; } = dialect;
    public SqlInterpolOptions Options { get; } = options ?? new() { Dialect = dialect.Kind };
    public Dictionary<string, object?> Parameters { get; } = new();
    internal ParseState State = new();

    internal struct ParseState
    {
        public SqlKeyword? CurrentKeyword;
        public bool IsInsideString;
        public int ParameterCount;
        public ISqlProjection? PendingAliasCapture { get; set; }
    }
}