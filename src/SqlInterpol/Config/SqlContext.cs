using SqlInterpol.Parsing;

namespace SqlInterpol.Config;

public class SqlContext(SqlBuilder builder, ISqlDialect dialect, ISqlInterpolationParser parser, ISqlSegmentRenderer renderer, SqlInterpolOptions? options = null) : ISqlParserContext
{
    public SqlBuilder Builder { get; } = builder;
    public ISqlDialect Dialect { get; } = dialect;
    public ISqlInterpolationParser Parser { get; } = parser;
    public ISqlSegmentRenderer Renderer { get; } = renderer;
    public SqlInterpolOptions Options { get; } = options ?? new() { Dialect = dialect.Kind };
    public IDictionary<string, object?> Parameters { get; } = new Dictionary<string, object?>();
    internal SqlParserState ParserState { get; } = new();

    ISqlParserState ISqlParserContext.ParserState => ParserState;
}