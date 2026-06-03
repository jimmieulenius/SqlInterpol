using SqlInterpol.Dialects;

namespace SqlInterpol;

/// <summary>
/// Configuration options for a <see cref="SqlBuilder"/> instance, controlling parameter naming,
/// collection rendering, enum formatting, and extensibility.
/// </summary>
/// <remarks>
/// Options are immutable for most properties (<c>init</c>-only). Use <c>with</c> expressions to
/// create modified copies. Pass an instance to a <see cref="SqlBuilder"/> constructor or set
/// defaults via <see cref="ISqlDialect.GetDefaultOptions"/>.
/// </remarks>
public record SqlInterpolOptions
{
    /// <summary>
    /// Gets the starting index used when generating parameter names (e.g. <c>0</c> → <c>@p0</c>, <c>1</c> → <c>@p1</c>).
    /// Defaults to <c>0</c>.
    /// </summary>
    public int ParameterIndexStart { get; init; } = 0;

    /// <summary>
    /// Gets an override for the dialect's default parameter prefix (e.g. <c>"@"</c>, <c>":"</c>).
    /// When <see langword="null"/>, the active dialect's <see cref="ISqlDialect.ParameterPrefix"/> is used.
    /// </summary>
    public string? ParameterPrefixOverride { get; init; }

    /// <summary>
    /// Gets or sets the separator inserted between collection elements.
    /// Defaults to <c>", "</c>.
    /// </summary>
    public string CollectionSeparator { get; set; } = ", ";

    /// <summary>
    /// Gets or sets how collection values are laid out in the SQL output.
    /// Defaults to <see cref="SqlCollectionLayout.Horizontal"/>.
    /// </summary>
    public SqlCollectionLayout CollectionLayout { get; set; } = SqlCollectionLayout.Horizontal;

    /// <summary>
    /// Gets or sets the number of spaces used for vertical collection indentation.
    /// Defaults to <c>4</c>.
    /// </summary>
    public int IndentSize { get; set; } = 4;

    /// <summary>
    /// Gets or sets how enum values are rendered in SQL output.
    /// Defaults to <see cref="SqlEnumFormat.Integer"/>.
    /// </summary>
    public SqlEnumFormat EnumFormat { get; set; } = SqlEnumFormat.Integer;

    /// <summary>
    /// Gets or sets a global override for the maximum number of parameters allowed per query.
    /// If left null, the engine defaults to the dialect's native maximum limit.
    /// </summary>
    public int? QueryParametersMaxCount { get; init; }

    /// <summary>
    /// Gets or sets a value indicating whether [CallerArgumentExpression] variable names 
    /// (e.g., `out var p`) should automatically be applied as SQL aliases for the generated entities.
    /// Defaults to false to ensure backward compatibility.
    /// </summary>
    public bool AutoAliasing { get; init; } = false;

    /// <summary>
    /// Gets the active dialect kind. Set automatically by <see cref="SqlBuilder"/> when constructing the context.
    /// </summary>
    public SqlDialectKind Dialect { get; init; } = SqlDialectKind.SqlServer;

    /// <summary>
    /// Gets an optional custom <see cref="ISqlInterpolationParser"/>.
    /// When <see langword="null"/>, <c>SqlInterpolationParser.Instance</c> is used.
    /// </summary>
    public ISqlInterpolationParser? Parser { get; init; }

    /// <summary>
    /// Gets an optional custom <see cref="ISqlSegmentRenderer"/>.
    /// When <see langword="null"/>, <c>SqlSegmentRenderer.Instance</c> is used.
    /// </summary>
    public ISqlSegmentRenderer? Renderer { get; init; }

    /// <summary>
    /// Returns the default <see cref="SqlInterpolOptions"/> for the specified dialect
    /// by delegating to <see cref="ISqlDialect.GetDefaultOptions"/>.
    /// </summary>
    /// <param name="dialect">The dialect whose defaults to retrieve.</param>
    /// <returns>A new <see cref="SqlInterpolOptions"/> with dialect-appropriate defaults.</returns>
    public static SqlInterpolOptions GetDefault(ISqlDialect dialect)
    {
        return dialect.GetDefaultOptions();
    }
}