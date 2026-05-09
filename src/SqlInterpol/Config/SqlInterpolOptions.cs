using SqlInterpol.Dialects;

namespace SqlInterpol.Config;

public record SqlInterpolOptions
{
    public int ParameterIndexStart { get; init; } = 0;
    public string? ParameterPrefixOverride { get; init; }
    public string CollectionSeparator { get; set; } = ", ";
    public SqlDialectKind Dialect { get; init; } = SqlDialectKind.SqlServer;
    public ISqlInterpolationParser? Parser { get; init; }
    public ISqlSegmentRenderer? Renderer { get; init; }

    public static SqlInterpolOptions GetDefault(ISqlDialect dialect)
    {
        return dialect.GetDefaultOptions();
    }
}