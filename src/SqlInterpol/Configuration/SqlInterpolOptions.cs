using SqlInterpol.Dialects;
using SqlInterpol.Pipeline;

namespace SqlInterpol.Configuration;

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
    /// Example: SqlInterpolOptions.DefaultFactory = () => new SqlInterpolOptions { CrossDialectSqlTranspilation = false };
    /// </summary>
    public static Func<SqlInterpolOptions>? DefaultFactory { get; set; }

    /// <summary>
    /// Gets the starting index used when generating parameter names (e.g. <c>0</c> → <c>@p0</c>, <c>1</c> → <c>@p1</c>).
    /// When <see langword="null"/>, the active dialect's default starting index is used.
    /// </summary>
    public int? ParameterIndexStart { get; init; }

    /// <summary>
    /// Gets an override for the dialect's default parameter prefix (e.g. <c>"@"</c>, <c>":"</c>).
    /// When <see langword="null"/>, the active dialect's <see cref="ISqlDialect.ParameterPrefix"/> is used.
    /// </summary>
    public string? ParameterPrefixOverride { get; init; }

    /// <summary>
    /// Gets or sets the separator inserted between collection elements.
    /// When <see langword="null"/>, defaults to <c>", "</c>.
    /// </summary>
    public string? CollectionSeparator { get; set; }

    /// <summary>
    /// Gets or sets how collection values are laid out in the SQL output.
    /// When <see langword="null"/>, defaults to <see cref="SqlCollectionLayout.Horizontal"/>.
    /// </summary>
    public SqlCollectionLayout? CollectionLayout { get; set; }

    /// <summary>
    /// Gets or sets the number of spaces used for vertical collection indentation.
    /// When <see langword="null"/>, defaults to <c>4</c>.
    /// </summary>
    public int? IndentSize { get; set; }

    /// <summary>
    /// Gets or sets how enum values are rendered in SQL output.
    /// When <see langword="null"/>, defaults to <see cref="SqlEnumFormat.Integer"/>.
    /// </summary>
    public SqlEnumFormat? EnumFormat { get; set; }

    /// <summary>
    /// Gets or sets a global override for the maximum number of parameters allowed per query.
    /// If left null, the engine defaults to the dialect's native maximum limit.
    /// </summary>
    public int? QueryParametersMaxCount { get; init; }

    /// <summary>
    /// Gets or sets a value indicating whether [CallerArgumentExpression] variable names 
    /// (e.g., `out var p`) should automatically be applied as SQL aliases for the generated entities.
    /// When <see langword="null"/>, defaults to <see langword="false"/> to ensure backward compatibility.
    /// </summary>
    public bool? EntityAutoAliasing { get; set; }

    /// <summary>
    /// When true, the engine will structurally transpile known Meta-SQL keywords 
    /// (like LIMIT / OFFSET) into the syntax required by the active database dialect.
    /// When <see langword="null"/>, defaults to <see langword="true"/>.
    /// </summary>
    public bool? CrossDialectSqlTranspilation { get; set; }

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
    /// A pipeline of custom lexical rules executed before the core preprocessor.
    /// Modify this list to inject advanced syntax recognition from extension packages.
    /// </summary>
    public List<ISqlPreprocessorRule> PreprocessorRules { get; } = new();

    /// <summary>
    /// The compilation pipeline modules. Modifying this list allows you to inject 
    /// custom SQL structural transformations. Duplicate rewriter types are safely ignored.
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
    /// A registry of custom keywords and their associated lexical tags.
    /// Extension packages can add keywords here so the Lexer automatically identifies them!
    /// </summary>
    public Dictionary<string, string[]> KeywordTags { get; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// An optional hook to capture query build metrics. 
    /// If null, telemetry allocation and timing are completely bypassed.
    /// </summary>
    public Action<SqlQueryTelemetry>? OnQueryBuilt { get; set; }
    
    /// <summary>
    /// Provides a resolved, strictly non-nullable view of the configuration options.
    /// </summary>
    public SqlInterpolOptionsValue Value => new SqlInterpolOptionsValue(this);

    /// <summary>
    /// Creates a new instance of options and automatically applies any globally registered extensions.
    /// </summary>
    public SqlInterpolOptions()
    {
        // Pull the deduplicated list of extensions from the registry!
        var globals = SqlExtensionRegistry.GetGlobalExtensions();
        for (int i = 0; i < globals.Count; i++)
        {
            globals[i].Register(this);
        }
    }

    /// <summary>
    /// Resolves the final options by coalescing the user's explicit overrides safely on top of the dialect's baseline defaults.
    /// </summary>
    /// <param name="dialect">The dialect whose defaults provide the baseline.</param>
    /// <param name="overrides">The optional user-provided overrides.</param>
    /// <returns>A new <see cref="SqlInterpolOptions"/> combining the dialect's defaults and user overrides.</returns>
    public static SqlInterpolOptions GetOptions(ISqlDialect dialect, SqlInterpolOptions? overrides = null)
    {
        var defaultOptions = dialect.GetDefaultOptions();
        
        if (overrides == null) 
        {
            return defaultOptions with { Dialect = dialect.Kind };
        }

        // Coalesce the user's explicit overrides safely on top of the defaults.
        // By using `overrides with { ... }`, we also preserve any lists (Rewriters, Rules) they modified!
        return overrides with 
        { 
            Dialect = dialect.Kind,
            ParameterIndexStart = overrides.ParameterIndexStart ?? defaultOptions.ParameterIndexStart,
            ParameterPrefixOverride = overrides.ParameterPrefixOverride ?? defaultOptions.ParameterPrefixOverride,
            CollectionSeparator = overrides.CollectionSeparator ?? defaultOptions.CollectionSeparator,
            CollectionLayout = overrides.CollectionLayout ?? defaultOptions.CollectionLayout,
            IndentSize = overrides.IndentSize ?? defaultOptions.IndentSize,
            EnumFormat = overrides.EnumFormat ?? defaultOptions.EnumFormat,
            QueryParametersMaxCount = overrides.QueryParametersMaxCount ?? defaultOptions.QueryParametersMaxCount,
            EntityAutoAliasing = overrides.EntityAutoAliasing ?? defaultOptions.EntityAutoAliasing,
            CrossDialectSqlTranspilation = overrides.CrossDialectSqlTranspilation ?? defaultOptions.CrossDialectSqlTranspilation,
            Preprocessor = overrides.Preprocessor ?? defaultOptions.Preprocessor,
            Renderer = overrides.Renderer ?? defaultOptions.Renderer,
            OnQueryBuilt = overrides.OnQueryBuilt ?? defaultOptions.OnQueryBuilt
        };
    }
}

/// <summary>
/// A lightweight wrapper providing non-nullable access to SQL options, returning safe 
/// defaults to satisfy the type system. (Note: Inside the engine, dialect defaults are 
/// already merged by GetOptions() before accessing this, so these defaults are purely fallback guarantees).
/// </summary>
public readonly struct SqlInterpolOptionsValue(SqlInterpolOptions opt)
{
    public int ParameterIndexStart => opt.ParameterIndexStart ?? 0;
    public string? ParameterPrefixOverride => opt.ParameterPrefixOverride;
    public string CollectionSeparator => opt.CollectionSeparator ?? ", ";
    public SqlCollectionLayout CollectionLayout => opt.CollectionLayout ?? SqlCollectionLayout.Horizontal;
    public int IndentSize => opt.IndentSize ?? 4;
    public SqlEnumFormat EnumFormat => opt.EnumFormat ?? SqlEnumFormat.Integer;
    public int QueryParametersMaxCount => opt.QueryParametersMaxCount ?? 999;
    public bool EntityAutoAliasing => opt.EntityAutoAliasing ?? false;
    public bool CrossDialectSqlTranspilation => opt.CrossDialectSqlTranspilation ?? true;
}