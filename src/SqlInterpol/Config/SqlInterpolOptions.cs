using SqlInterpol.Parsing;

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
    /// A factory method used to generate the default options for every newly created SqlBuilder.
    /// Configure this once at application startup.
    /// Example: SqlInterpolOptions.DefaultFactory = () => new SqlInterpolOptions { MetaSqlTranspilation = false };
    /// </summary>
    public static Func<SqlInterpolOptions>? DefaultFactory { get; set; }

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
    public bool EntityAutoAliasing { get; set; } = false;

    /// <summary>
    /// When true, the engine will structurally transpile known Meta-SQL keywords 
    /// (like LIMIT / OFFSET) into the syntax required by the active database dialect.
    /// Default is true.
    /// </summary>
    public bool MetaSqlTranspilation { get; set; } = true;

    /// <summary>
    /// Gets the active dialect kind. Set automatically by <see cref="SqlBuilder"/> when constructing the context.
    /// </summary>
    public SqlDialectKind Dialect { get; init; } = SqlDialectKind.SqlServer;

    /// <summary>
    /// Gets an optional custom <see cref="ISqlSegmentPreprocessor"/>.
    /// When <see langword="null"/>, <c>SqlSegmentPreprocessor.Instance</c> is used.
    /// </summary>
    public ISqlSegmentPreprocessor? Preprocessor { get; init; }

    /// <summary>
    /// The compilation pipeline modules. Modifying this list allows you to inject 
    /// custom SQL AST transformations. Duplicate rewriter types are safely ignored.
    /// </summary>
    public SqlSegmentRewriterCollection Rewriters { get; } = new SqlSegmentRewriterCollection
    {
        new SqlCoreSyntaxRewriter(),
        new SqlSelectIntoRewriter(),
        new SqlMultiTableDmlRewriter()
    };

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