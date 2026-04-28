namespace SqlInterpol.Config;

public class SqlInterpolOptions
{
    public SqlDialectKind Dialect { get; set; } = SqlDialectKind.SqlServer;

    public bool UsePositionalParameters { get; set; } = false;

    public int IndentSize { get; set; } = 2;

    public static SqlInterpolOptions Default => new();
}