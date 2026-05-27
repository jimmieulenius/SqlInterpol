using System.Runtime.CompilerServices;
using SqlInterpol.Parsing;

namespace SqlInterpol;

/// <summary>
/// The primary entry point for building parameterized, dialect-aware SQL queries using C# interpolated strings.
/// </summary>
/// <remarks>
/// <para>
/// A <see cref="SqlBuilder"/> is created once per dialect via one of the static factory methods
/// (e.g. <see cref="PostgreSql"/>, <see cref="SqlServer"/>) and then reused to build multiple queries.
/// Each call to <see cref="Build(bool)"/> renders the accumulated segments and, by default, resets
/// the builder for the next query.
/// </para>
/// <para>
/// SQL is written using standard C# interpolated string syntax. Every interpolated value is
/// automatically extracted as a named <c>DbParameter</c> — no string concatenation, no SQL injection.
/// </para>
/// <example>
/// <code>
/// var db = SqlBuilder.PostgreSql();
/// var query = db.Query&lt;Product&gt;((p) => db.Append($"""
///     SELECT {p[x => x.Id]}, {p[x => x.Name]}
///     FROM {p}
///     WHERE {p[x => x.IsActive]} = {true}
///       AND {p[x => x.Price]} > {minPrice}
/// """)).Build();
///
/// // query.Sql        => SELECT "p"."Id", "p"."Name" FROM "Products" AS "p" WHERE "p"."IsActive" = @p0 AND "p"."Price" > @p1
/// // query.Parameters => { "@p0": true, "@p1": 42 }
/// </code>
/// </example>
/// </remarks>
public partial class SqlBuilder : ISqlEntityRegistry
{
    private List<SqlSegment> _segments = [];
    private readonly List<ISqlEntity> _entities = [];

    /// <summary>Gets the <see cref="SqlContext"/> holding the dialect, parser, renderer, and options for this builder.</summary>
    public SqlContext Context { get; }
    private ISqlInterpolationParser Parser => Context.Parser;
    private ISqlSegmentRenderer Renderer => Context.Renderer;
    internal IReadOnlyList<SqlSegment> Segments => _segments;

    /// <summary>
    /// Initializes a new <see cref="SqlBuilder"/> for the specified dialect.
    /// </summary>
    /// <param name="dialect">The SQL dialect that controls identifier quoting, feature support, and segment rewriting.</param>
    /// <param name="options">
    /// Optional configuration options. When <see langword="null"/>, dialect-default options are resolved via
    /// <see cref="SqlInterpolOptions.GetDefault"/>.
    /// </param>
    public SqlBuilder(ISqlDialect dialect, SqlInterpolOptions? options = null)
    {
        var baseOptions = options ?? SqlInterpolOptions.GetDefault(dialect);
        var finalOptions = baseOptions with { Dialect = dialect.Kind };
        var parser = finalOptions.Parser ?? SqlInterpolationParser.Instance;
        var renderer = options?.Renderer ?? SqlSegmentRenderer.Instance;
        Context = new SqlContext(this, dialect, parser, renderer, finalOptions);
    }

    private SqlBuilder Append(string? value)
    {
        if (string.IsNullOrEmpty(value)) return this;
        _segments.Add(ProcessLiteral(value));

        return this;
    }

    /// <summary>
    /// Appends an interpolated SQL fragment to the current query being built.
    /// Interpolated values are automatically parameterized; SQL literals are passed through as-is.
    /// </summary>
    /// <param name="handler">
    /// The interpolated string handler that captures SQL text literals and typed interpolation holes.
    /// C# constructs this automatically when you write <c>$"... {value} ..."</c>.
    /// </param>
    /// <returns>The current <see cref="SqlBuilder"/> instance for method chaining.</returns>
    public SqlBuilder Append([InterpolatedStringHandlerArgument("")] ref SqlQueryInterpolatedStringHandler handler)
    {
        handler.TransferSegments(_segments);

        return this;
    }

    /// <summary>
    /// Appends a newline to the current query.
    /// </summary>
    /// <returns>The current <see cref="SqlBuilder"/> instance for method chaining.</returns>
    public SqlBuilder AppendLine() => Append(Environment.NewLine);

    /// <summary>
    /// Appends an interpolated SQL fragment followed by a newline to the current query.
    /// </summary>
    /// <param name="handler">
    /// The interpolated string handler that captures SQL text literals and typed interpolation holes.
    /// </param>
    /// <returns>The current <see cref="SqlBuilder"/> instance for method chaining.</returns>
    public SqlBuilder AppendLine([InterpolatedStringHandlerArgument("")] ref SqlQueryInterpolatedStringHandler handler)
    {
        Append(ref handler);

        return AppendLine();
    }

    /// <summary>
    /// Clears all accumulated segments and resets the builder's internal state, making it ready for a new query.
    /// </summary>
    /// <returns>The current <see cref="SqlBuilder"/> instance for method chaining.</returns>
    public virtual SqlBuilder Clear()
    {
        _segments.Clear();
        Context.Reset();

        return this;
    }

    /// <summary>
    /// Captures the SQL written by <paramref name="action"/> into an isolated <see cref="ISqlQuery"/> scope,
    /// without affecting the segments accumulated on the outer builder.
    /// Use this to construct subqueries or reusable query fragments.
    /// </summary>
    /// <param name="action">The action that appends SQL to the builder; its output is captured separately.</param>
    /// <returns>An <see cref="ISqlQuery"/> holding the segments written inside <paramref name="action"/>.</returns>
    public ISqlQuery Query(Action action)
    {
        var mainSegments = _segments;
        var scopedSegments = new List<SqlSegment>();

        try
        {
            _segments = scopedSegments;
            action();
        }
        finally
        {
            _segments = mainSegments;
        }

        return new SqlQuery(this, scopedSegments);
    }

    /// <summary>
    /// Builds all accumulated segments into a <see cref="SqlQueryResult"/> containing the rendered SQL string
    /// and the dictionary of extracted parameters.
    /// </summary>
    /// <param name="arguments">An optional anonymous object containing global template arguments (e.g., <c>new { tenantId = 5 }</c>).</param>
    /// <param name="clear">
    /// When <see langword="true"/> (default), <see cref="Clear"/> is called after building so the builder
    /// is immediately ready for the next query.
    /// </param>
    /// <returns>The <see cref="SqlQueryResult"/> ready for execution via Dapper, EF Core, or raw ADO.NET.</returns>
    /// <exception cref="SqlDialectException">Thrown when the query uses features not supported by the active dialect.</exception>
    /// <exception cref="ArgumentException">Thrown when a template argument is not provided.</exception>
    public SqlQueryResult Build(object? arguments = null, bool clear = true)
    {
        var result = BuildSegments(_segments, arguments);

        if (clear)
        {
            Clear();
        }

        return result;
    }

    /// <summary>
    /// Builds a previously captured <see cref="ISqlQuery"/> into a <see cref="SqlQueryResult"/>.
    /// The builder's own accumulated segments are not affected.
    /// </summary>
    /// <param name="query">The captured query to render.</param>
    /// <param name="arguments">An optional anonymous object containing global template arguments.</param>
    /// <returns>The <see cref="SqlQueryResult"/> ready for execution.</returns>
    /// <exception cref="SqlDialectException">Thrown when the query uses features not supported by the active dialect.</exception>
    public SqlQueryResult Build(ISqlQuery query, object? arguments = null) 
        => BuildSegments(query.Segments, arguments);

    private SqlQueryResult BuildSegments(IReadOnlyList<SqlSegment> segmentsToBuild, object? arguments)
    {
        // 1. GLOBAL TEMPLATE ARGUMENT RESOLUTION (Pre-pass)
        List<SqlSegment>? resolvedSegments = null;
        IReadOnlyDictionary<string, Func<object, object?>>? getters = null;

        if (arguments != null)
        {
            getters = SqlMetadataRegistry.GetArgumentGetters(arguments.GetType());
        }

        for (int i = 0; i < segmentsToBuild.Count; i++)
        {
            var segment = segmentsToBuild[i];
            
            if (segment.Type == SqlSegmentType.Raw && segment.Value is SqlArgumentFragment argFragment)
            {
                // Lazy clone on first argument encountered
                resolvedSegments ??= [.. segmentsToBuild];

                string argName = argFragment.Name;
                bool resolved = false;

                if (getters != null && getters.TryGetValue(argName, out var getter))
                {
                    object? val = getter(arguments!);
                    string paramKey = Context.AddParameter(val);
                    
                    // Replace in the mutable copy
                    resolvedSegments[i] = new SqlSegment(SqlSegmentType.Raw, paramKey);
                    resolved = true;
                }

                if (!resolved)
                {
                    throw new ArgumentException(
                        $"The SQL template requires an argument named '{argName}', but it was not provided globally or locally.");
                }
            }
        }

        // If we resolved any arguments, use the mutable copy for the rest of the pipeline
        var finalInputSegments = resolvedSegments != null ? (IReadOnlyList<SqlSegment>)resolvedSegments : segmentsToBuild;

        // 2. DIALECT FEATURE VALIDATION
        var errors = new List<string>();
        foreach (var segment in finalInputSegments)
        {
            SqlFeature? requiredFeature = null;
            string? featureName = null;

            if (segment.Value is ISqlFeatureRequirement req)
            {
                requiredFeature = req.RequiredFeature;
                featureName = req.FeatureName;
            }
            else if (segment.Tag == SqlSegmentTag.ForUpdateKeyword)
            {
                requiredFeature = SqlFeature.ForUpdate;
                featureName = "FOR UPDATE";
            }
            else if (segment.Tag == SqlSegmentTag.ForShareKeyword)
            {
                requiredFeature = SqlFeature.ForShare;
                featureName = "FOR SHARE";
            }
            else if (segment.Tag == SqlSegmentTag.ReturningKeyword)
            {
                requiredFeature = SqlFeature.Returning;
                featureName = "RETURNING";
            }
            else if (segment.Tag == SqlSegmentTag.OnConflictKeyword)
            {
                requiredFeature = SqlFeature.OnConflict;
                featureName = "ON CONFLICT";
            }

            if (requiredFeature.HasValue && !Context.Dialect.SupportedFeatures.Contains(requiredFeature.Value))
            {
                errors.Add($"'{featureName}' is not supported by {Context.Dialect.Kind}.");
            }
        }

        if (errors.Count > 0)
        {
            throw new SqlDialectException($"Dialect capabilities validation failed:{Environment.NewLine}- " + string.Join($"{Environment.NewLine}- ", errors));
        }

        // 3. REWRITE & RENDER
        var finalSegments = Context.Dialect.RewriteSegments(finalInputSegments).ToList();
        var vsb = new ValueStringBuilder(stackalloc char[2048]);

        try
        {
            for (int i = 0; i < finalSegments.Count; i++)
            {
                vsb.Append(Renderer.Render(Context, finalSegments[i], i, finalSegments) ?? string.Empty);
            }
            return new SqlQueryResult(vsb.ToString(), Context.Parameters.AsReadOnly());
        }
        finally
        {
            vsb.Dispose();
        }
    }

    internal SqlSegment ProcessLiteral(string value)
    {
        string? tag = Parser.ProcessLiteral(Context, value.AsSpan());
        return new SqlSegment(SqlSegmentType.Literal, value, null, tag);
    }

    internal SqlSegment ProcessValue(object? value) => Parser.ProcessValue(Context, value);

    internal void AppendSegment(SqlSegment segment)
    {
        _segments.Add(segment);
    }

    ISqlEntity<T> ISqlEntityRegistry.RegisterEntity<T>(string? name, string? schema, string? alias)
    {
        var entity = CreateEntity<T>(name, schema, alias);
        _entities.Add(entity);

        return entity;
    }

    internal ISqlEntity<T> CreateEntity<T>(string? name = null, string? schema = null, string? alias = null)
    {
        var meta = SqlMetadataRegistry.GetMetadata<T>();
        string physicalName = name ?? meta.Name;
        string? physicalSchema = schema ?? meta.Schema;

        return meta.Type switch
        {
            SqlEntityType.View => new SqlView<T>(physicalName, physicalSchema, alias),
            _ => new SqlTable<T>(physicalName, physicalSchema, alias)
        };
    }
}