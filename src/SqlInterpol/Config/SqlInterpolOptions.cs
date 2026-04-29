namespace SqlInterpol.Config;

public delegate ISqlParser SqlParserFactory(IServiceProvider serviceProvider);

public class SqlInterpolOptions
{
    public SqlDialectKind Dialect { get; set; } = SqlDialectKind.SqlServer;

    public SqlParserFactory? ParserFactory { get; set; }

    public static SqlInterpolOptions Default => new();
}