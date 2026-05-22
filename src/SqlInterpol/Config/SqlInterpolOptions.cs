using SqlInterpol.Dialects;

namespace SqlInterpol;

public record SqlInterpolOptions
{
    public int ParameterIndexStart { get; init; } = 0;
    public string? ParameterPrefixOverride { get; init; }
    public string CollectionSeparator { get; set; } = ", ";
    public SqlCollectionLayout CollectionLayout { get; set; } = SqlCollectionLayout.Horizontal;
    public int IndentSize { get; set; } = 4;
    public SqlEnumFormat EnumFormat { get; set; } = SqlEnumFormat.Integer;
    public SqlDialectKind Dialect { get; init; } = SqlDialectKind.SqlServer;
    public ISqlInterpolationParser? Parser { get; init; }
    public ISqlSegmentRenderer? Renderer { get; init; }

    public static SqlInterpolOptions GetDefault(ISqlDialect dialect)
    {
        return dialect.GetDefaultOptions();
    }
}