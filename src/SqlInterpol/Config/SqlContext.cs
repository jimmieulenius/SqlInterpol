using SqlInterpol.Parsing;

namespace SqlInterpol.Config;

public class SqlContext(SqlBuilder builder, ISqlDialect dialect, ISqlParser parser, ISqlSegmentRenderer renderer, SqlInterpolOptions? options = null)
{
    public SqlBuilder Builder { get; } = builder;
    public ISqlDialect Dialect { get; } = dialect;
    public ISqlParser Parser { get; } = parser;
    public ISqlSegmentRenderer Renderer { get; } = renderer;
    public SqlInterpolOptions Options { get; } = options ?? new() { Dialect = dialect.Kind };
    public Dictionary<string, object?> Parameters { get; } = [];
    internal SqlParseState ParseState { get; } = new();
}