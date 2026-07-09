using System.Runtime.CompilerServices;
using SqlInterpol.Parsing;

namespace SqlInterpol;

/// <summary>
/// The primary entry point for building parameterized, dialect-aware SQL queries using C# interpolated strings.
/// </summary>
public partial class SqlBuilder : ISqlEntityRegistry, ISqlGeneratorBuilder
{
    private List<SqlSegment> _segments = [];
    private readonly List<ISqlEntity> _entities = [];

    /// <summary>
    /// Tracks variable names mapped from [CallerArgumentExpression] for zero-allocation property routing.
    /// </summary>
    internal Dictionary<string, ISqlEntityBase> ScopedVariables { get; } = new(StringComparer.Ordinal);

    /// <summary>
    /// Gets the <see cref="SqlContext"/> holding the dialect, renderer, options, and parameters for this builder.
    /// </summary>
    public SqlContext Context { get; }
    
    private ISqlSegmentRenderer Renderer => Context.Renderer;
    internal IReadOnlyList<SqlSegment> Segments => _segments;
    
    /// <summary>
    /// Gets the current index of the segment being rendered within the timeline. 
    /// Used natively by dialects to calculate subquery and CTE declaration layouts.
    /// </summary>
    public int CurrentRenderIndex { get; internal set; }

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
        var renderer = options?.Renderer ?? SqlSegmentRenderer.Instance;
        
        Context = new SqlContext(this, dialect, renderer, finalOptions);
    }

    private SqlBuilder Append(string? value)
    {
        if (string.IsNullOrEmpty(value)) return this;
        
        // Pure, O(1) literal addition. No parser intercepting or text modification.
        _segments.Add(new SqlSegment(SqlSegmentType.Literal, value));

        return this;
    }

    /// <summary>
    /// Appends an interpolated SQL fragment to the current query being built.
    /// Interpolated values are automatically parameterized; SQL literals are passed through as-is.
    /// </summary>
    /// <param name="handler">
    /// The interpolated string handler that captures SQL text literals and typed interpolation holes.
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
    /// Clears all accumulated segments, variables, and resets the builder's internal parameter state, making it ready for a new query.
    /// </summary>
    /// <returns>The current <see cref="SqlBuilder"/> instance for method chaining.</returns>
    public virtual SqlBuilder Clear()
    {
        _segments.Clear();
        Context.Reset();

        return this;
    }

    /// <summary>
    /// Parses an inline interpolated SQL string into a frozen, lightweight AST fragment
    /// without modifying this builder's master statement stream.
    /// </summary>
    /// <param name="handler">The compiler-routed interpolation handler tracking the token stream.</param>
    public ISqlFragment Fragment([InterpolatedStringHandlerArgument("")] ref SqlQueryInterpolatedStringHandler handler)
    {
        var segments = new List<SqlSegment>();
        
        // Slices the parsed tokens out of the zero-allocation handler
        handler.TransferSegments(segments);
        
        return new SqlSegmentCollectionFragment(segments);
    }

    /// <summary>
    /// Builds a frozen AST fragment imperatively via a callback. Perfect for dynamic loops
    /// (e.g., building a variable WHERE filter list) while sharing the parent's entity scopes.
    /// </summary>
    /// <param name="buildAction">The delegate used to conditionally append fragments.</param>
    public ISqlFragment Fragment(Action<SqlBuilder> buildAction)
    {
        // 1. Create an isolated sub-builder sharing the exact same execution context
        var subBuilder = new SqlBuilder(this.Context.Dialect, this.Context.Options);
        
        // 2. Clone the current scoped variables so local table aliases map flawlessly
        foreach (var kvp in this.ScopedVariables)
        {
            subBuilder.ScopedVariables[kvp.Key] = kvp.Value;
        }

        // 3. Execute the user's dynamic structure rules
        buildAction(subBuilder);

        // 4. Extract the isolated segments and freeze them into an immutable collection
        // Note: Assumes your SqlBuilder exposes its internal segment list via a method like GetSegments()
        return new SqlSegmentCollectionFragment(subBuilder.Segments);
    }

    /// <summary>
    /// Compiles an interpolated SQL string into a high-performance, reusable <see cref="ISqlTemplate"/>.
    /// The resulting template bypasses AST compilation during execution, natively injecting arguments in O(1) time.
    /// </summary>
    public ISqlTemplate Template([InterpolatedStringHandlerArgument("")] ref SqlQueryInterpolatedStringHandler handler)
    {
        var segments = new List<SqlSegment>();
        handler.TransferSegments(segments);

        var preprocessor = Context.Options.Preprocessor ?? SqlSegmentPreprocessor.Instance;
        var pipeline = new SqlCompilationPipeline(preprocessor, Context.Options.Rewriters);
        
        var compiledSegments = pipeline.Compile(segments, Context);

        var vsb = new SqlInterpol.Parsing.ValueStringBuilder(stackalloc char[2048]);
        try
        {
            var templateArgs = new List<SqlTemplateArgument>();
            int holeIndex = 0;

            for (int i = 0; i < compiledSegments.Count; i++)
            {
                var segment = compiledSegments[i];
                
                if (segment.Type == SqlSegmentType.Raw && segment.Value is SqlArgumentFragment argFrag)
                {
                    // Convert Sql.Arg("name") into a {X} format hole
                    vsb.Append($"{{{holeIndex++}}}");
                    templateArgs.Add(new SqlTemplateArgument(argFrag.Name));
                }
                else if (segment.Type == SqlSegmentType.Unresolved || segment.Type == SqlSegmentType.Parameter)
                {
                    // Convert statically captured local variables into a {X} format hole seamlessly
                    vsb.Append($"{{{holeIndex++}}}");
                    templateArgs.Add(new SqlTemplateArgument(segment.Value));
                }
                else
                {
                    // Render structural text, fully escaping braces to prevent string.Format crashes
                    CurrentRenderIndex = i;
                    var rendered = Renderer.Render(Context, segment, i, compiledSegments);
                    if (rendered != null)
                    {
                        rendered = rendered.Replace("{", "{{").Replace("}", "}}");
                        vsb.Append(rendered);
                    }
                }
            }

            return new SqlTemplate(vsb.ToString(), templateArgs.ToArray());
        }
        finally
        {
            vsb.Dispose();
        }
    }

    /// <summary>
    /// Compiles an interpolated SQL string into a high-performance, reusable <see cref="ISqlTemplate"/>,
    /// assigning it to the output parameter and returning the builder to allow fluent chaining.
    /// </summary>
    public SqlBuilder Template(out ISqlTemplate template, [InterpolatedStringHandlerArgument("")] ref SqlQueryInterpolatedStringHandler handler)
    {
        template = Template(ref handler);
        return this;
    }

    /// <summary>
    /// Captures the SQL written by <paramref name="action"/> into an isolated buildable <see cref="ISqlQuery"/> scope,
    /// without affecting the segments accumulated on the outer builder.
    /// </summary>
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

        // Clean, stateless AST node creation!
        return new SqlQuery(scopedSegments);
    }

    /// <summary>
    /// Builds all accumulated segments into a <see cref="SqlQueryResult"/> containing the rendered SQL string
    /// and the dictionary of extracted parameters.
    /// </summary>
    /// <param name="arguments">An optional anonymous object containing global template arguments.</param>
    /// <param name="clear">When <see langword="true"/> (default), <see cref="Clear"/> is called after building.</param>
    /// <returns>The <see cref="SqlQueryResult"/> ready for execution.</returns>
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
    /// Builds a previously captured buildable <see cref="ISqlQuery"/> into a <see cref="SqlQueryResult"/>.
    /// </summary>
    /// <param name="query">The isolated query to build.</param>
    /// <param name="arguments">An optional anonymous object containing global template arguments.</param>
    /// <returns>The <see cref="SqlQueryResult"/> ready for execution.</returns>
    public SqlQueryResult Build(ISqlQuery query, object? arguments = null) 
        => BuildSegments(query.Segments, arguments);

    private SqlQueryResult BuildSegments(IReadOnlyList<SqlSegment> segmentsToBuild, object? arguments)
    {
        // ---------------------------------------------------------------------
        // 1. GLOBAL TEMPLATE ARGUMENT RESOLUTION (Pre-pass)
        // ---------------------------------------------------------------------
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
                resolvedSegments ??= [.. segmentsToBuild];

                string argName = argFragment.Name;
                bool resolved = false;

                if (getters != null && getters.TryGetValue(argName, out var getter))
                {
                    object? val = getter(arguments!);
                    // Process as Unresolved so standard parameter routing kicks in
                    resolvedSegments[i] = new SqlSegment(SqlSegmentType.Unresolved, val);
                    resolved = true;
                }

                if (!resolved)
                {
                    throw new ArgumentException(
                        $"The SQL template requires an argument named '{argName}', but it was not provided globally or locally.");
                }
            }
        }

        var finalInputSegments = resolvedSegments != null ? (IReadOnlyList<SqlSegment>)resolvedSegments : segmentsToBuild;

        // ---------------------------------------------------------------------
        // 2. COMPILE PIPELINE (Lexical Analysis & Semantic Rewriters)
        // ---------------------------------------------------------------------
        var preprocessor = Context.Options.Preprocessor ?? SqlSegmentPreprocessor.Instance;
        var pipeline = new SqlCompilationPipeline(preprocessor, Context.Options.Rewriters);
        
        var compiledSegments = pipeline.Compile(finalInputSegments, Context);

        // ---------------------------------------------------------------------
        // 3. DIALECT FEATURE VALIDATION
        // ---------------------------------------------------------------------
        var errors = new List<string>();
        foreach (var segment in compiledSegments)
        {
            SqlFeature? requiredFeature = null;
            string? featureName = null;

            if (segment.Value is ISqlFeatureRequirement req)
            {
                requiredFeature = req.RequiredFeature;
                featureName = req.FeatureName;
            }
            else if (segment.HasTag(SqlSegmentTag.ForUpdateKeyword))
            {
                requiredFeature = SqlFeature.ForUpdate;
                featureName = "FOR UPDATE";
            }
            else if (segment.HasTag(SqlSegmentTag.ForShareKeyword))
            {
                requiredFeature = SqlFeature.ForShare;
                featureName = "FOR SHARE";
            }
            else if (segment.HasTag(SqlSegmentTag.ReturningKeyword))
            {
                requiredFeature = SqlFeature.Returning;
                featureName = "RETURNING";
            }
            else if (segment.HasTag(SqlSegmentTag.OnConflictKeyword))
            {
                requiredFeature = SqlFeature.OnConflict;
                featureName = "ON CONFLICT";
            }
            else if (segment.HasTag(SqlSegmentTag.DeleteAsKeyword))
            {
                requiredFeature = SqlFeature.DeleteAs;
                featureName = "DELETE with target alias";
            }
            else if (segment.HasTag(SqlSegmentTag.UpdateAsKeyword))
            {
                requiredFeature = SqlFeature.UpdateAs;
                featureName = "UPDATE with target alias";
            }

            if (requiredFeature.HasValue && !Context.Dialect.SupportedFeatures.Contains(requiredFeature.Value))
            {
                errors.Add($"'{featureName}' is not supported by {Context.Dialect.Kind}.");
            }
        }

        if (errors.Count > 0)
        {
            throw new SqlDialectException(
                Context.Dialect.Kind.ToString(), 
                "Multiple Operations", 
                $"Dialect capabilities validation failed:{Environment.NewLine}- " + string.Join($"{Environment.NewLine}- ", errors));
        }

        // ---------------------------------------------------------------------
        // 4. RENDER TO TEXT
        // ---------------------------------------------------------------------
        var vsb = new SqlInterpol.Parsing.ValueStringBuilder(stackalloc char[2048]);

        try
        {
            for (int i = 0; i < compiledSegments.Count; i++)
            {
                CurrentRenderIndex = i; // Save rendering track index natively for Subquery Declaration layouts
                vsb.Append(Renderer.Render(Context, compiledSegments[i], i, compiledSegments) ?? string.Empty);
            }
            return new SqlQueryResult(vsb.ToString(), Context.Parameters.AsReadOnly());
        }
        finally
        {
            vsb.Dispose();
        }
    }

    internal void AppendSegment(SqlSegment segment)
    {
        _segments.Add(segment);
    }

    /// <summary>
    /// Processes an interpolated value into a typed SQL segment for the preprocessor.
    /// </summary>
    internal SqlSegment ProcessValue(object? value)
    {
        if (value is SqlSegment segment) 
            return segment;

        // Entities go to the preprocessor as Unresolved so they can be securely mapped (e.g. SELECT *)
        if (value is ISqlEntityBase entity) 
            return new SqlSegment(SqlSegmentType.Unresolved, entity);

        // Explicit structural AST nodes bypass parameterization
        if (value is ISqlFragment fragment) 
            return new SqlSegment(SqlSegmentType.Raw, fragment);

        // Primitives, DTOs, and Iterables are deferred for parameter extraction
        return new SqlSegment(SqlSegmentType.Unresolved, value);
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