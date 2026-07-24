using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using SqlInterpol.Configuration;
using SqlInterpol.Execution;
using SqlInterpol.Pipeline;
using SqlInterpol.Schema;
using SqlInterpol.Segments;

namespace SqlInterpol;

/// <summary>
/// The primary entry point for building parameterized, dialect-aware SQL queries using C# interpolated strings.
/// </summary>
public partial class SqlBuilder : ISqlEntityRegistry
{
    private List<SqlSegment> _segments = [];
    private readonly List<ISqlEntityBase> _entities = [];

    /// <summary>
    /// Tracks variable names mapped from caller argument expressions for zero-allocation property routing.
    /// </summary>
    internal Dictionary<string, ISqlEntityBase> ScopedVariables { get; } = new(StringComparer.Ordinal);

    /// <summary>
    /// Gets the context holding the dialect, renderer, options, and parameters for this builder.
    /// </summary>
    public SqlContext Context { get; }
    
    private ISqlSegmentRenderer Renderer => Context.Options?.Renderer ?? SqlSegmentRenderer.Instance;
    
    internal IReadOnlyList<SqlSegment> Segments => _segments;
    
    /// <summary>
    /// Gets the current index of the segment being rendered within the timeline. 
    /// Used natively by dialects to calculate subquery and CTE declaration layouts.
    /// </summary>
    public int CurrentRenderIndex { get; internal set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="SqlBuilder"/> class for the specified dialect.
    /// </summary>
    /// <param name="dialect">The SQL dialect that controls identifier quoting, feature support, and segment rewriting.</param>
    /// <param name="options">Optional configuration options. Falls back to dialect defaults if null.</param>
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
        
        _segments.Add(new SqlSegment(SqlSegmentType.Literal, value));

        return this;
    }

    /// <summary>
    /// Appends an interpolated SQL string to the current query being built.
    /// Interpolated values are automatically parameterized; SQL literals are passed through as-is.
    /// </summary>
    /// <param name="handler">The interpolated string handler capturing SQL text literals and typed interpolation holes.</param>
    /// <returns>The current builder instance for method chaining.</returns>
    public SqlBuilder Append([InterpolatedStringHandlerArgument("")] ref SqlQueryInterpolatedStringHandler handler)
    {
        handler.TransferSegments(_segments);
        return this;
    }

    /// <summary>
    /// Appends a newline to the current query.
    /// </summary>
    /// <returns>The current builder instance for method chaining.</returns>
    public SqlBuilder AppendLine() => Append(Environment.NewLine);

    /// <summary>
    /// Appends an interpolated SQL string followed by a newline to the current query.
    /// </summary>
    /// <param name="handler">The interpolated string handler capturing SQL text literals and typed interpolation holes.</param>
    /// <returns>The current builder instance for method chaining.</returns>
    public SqlBuilder AppendLine([InterpolatedStringHandlerArgument("")] ref SqlQueryInterpolatedStringHandler handler)
    {
        Append(ref handler);
        return AppendLine();
    }

    /// <summary>
    /// Clears all accumulated segments and resets the builder's internal parameter state, making it ready for a new query.
    /// </summary>
    /// <returns>The current builder instance for method chaining.</returns>
    public virtual SqlBuilder Clear()
    {
        _segments.Clear();
        Context.Reset();
        return this;
    }

    /// <summary>
    /// Parses an inline interpolated SQL string into a frozen, lightweight intermediate representation fragment
    /// without modifying this builder's master statement stream.
    /// </summary>
    /// <param name="handler">The compiler-routed interpolation handler tracking the token stream.</param>
    /// <returns>A fragment representing the parsed string.</returns>
    public ISqlFragment Fragment([InterpolatedStringHandlerArgument("")] ref SqlQueryInterpolatedStringHandler handler)
    {
        var segments = new List<SqlSegment>();
        handler.TransferSegments(segments);
        
        return new SqlSegmentCollectionFragment(segments);
    }

    /// <summary>
    /// Builds a frozen sequence fragment imperatively via a callback. Useful for dynamic loops 
    /// while securely sharing the parent's entity scopes.
    /// </summary>
    /// <param name="buildAction">The delegate used to conditionally append fragments.</param>
    /// <returns>A fragment representing the parsed sequence.</returns>
    public ISqlFragment Fragment(Action<SqlBuilder> buildAction)
    {
        var subBuilder = new SqlBuilder(Context.Dialect, Context.Options);
        
        foreach (var kvp in ScopedVariables)
        {
            subBuilder.ScopedVariables[kvp.Key] = kvp.Value;
        }

        buildAction(subBuilder);

        return new SqlSegmentCollectionFragment(subBuilder.Segments);
    }

    /// <summary>
    /// Compiles an interpolated SQL string into a high-performance, reusable template.
    /// The resulting template bypasses stream processing during execution, natively injecting arguments in O(1) time.
    /// </summary>
    /// <param name="handler">The compiler-routed interpolation handler tracking the token stream.</param>
    /// <returns>The compiled SQL template.</returns>
    public ISqlTemplate Template([InterpolatedStringHandlerArgument("")] ref SqlQueryInterpolatedStringHandler handler)
    {
        var segments = new List<SqlSegment>();
        handler.TransferSegments(segments);

        var preprocessor = Context.Options.Preprocessor ?? SqlSegmentPreprocessor.Instance;
        var pipeline = new SqlPipeline(preprocessor, Context.Options.Rewriters);
        
        var compiledSegments = pipeline.Process(segments, Context);

        var vsb = new ValueStringBuilder(stackalloc char[2048]);
        try
        {
            var templateArgs = new List<SqlTemplateArgument>();
            int holeIndex = 0;

            for (int i = 0; i < compiledSegments.Count; i++)
            {
                var segment = compiledSegments[i];
                
                if (segment.Type == SqlSegmentType.Raw && segment.Value is SqlArgumentFragment argFrag)
                {
                    vsb.Append($"{{{holeIndex++}}}");
                    templateArgs.Add(new SqlTemplateArgument(argFrag.Name));
                }
                else if (segment.Type == SqlSegmentType.Unresolved || segment.Type == SqlSegmentType.Parameter)
                {
                    vsb.Append($"{{{holeIndex++}}}");
                    templateArgs.Add(new SqlTemplateArgument(segment.Value));
                }
                else
                {
                    CurrentRenderIndex = i;
                    var rendered = Renderer.Render(Context, segment, i, compiledSegments);
                    if (rendered != null)
                    {
                        rendered = rendered.Replace("{", "{{").Replace("}", "}}");
                        vsb.Append(rendered);
                    }
                }
            }

#pragma warning disable SQLIA10 
            return new SqlTemplate(vsb.ToString(), templateArgs.ToArray());
#pragma warning restore SQLIA10
        }
        finally
        {
            vsb.Dispose();
        }
    }

    /// <summary>
    /// Compiles an interpolated SQL string into a high-performance, reusable template,
    /// assigning it to the output parameter and returning the builder to allow fluent chaining.
    /// </summary>
    /// <param name="template">The compiled SQL template.</param>
    /// <param name="handler">The compiler-routed interpolation handler tracking the token stream.</param>
    /// <returns>The current builder instance for method chaining.</returns>
    public SqlBuilder Template(out ISqlTemplate template, [InterpolatedStringHandlerArgument("")] ref SqlQueryInterpolatedStringHandler handler)
    {
#pragma warning disable SQLIA07
        template = Template(ref handler);
#pragma warning restore SQLIA07
        return this;
    }

    /// <summary>
    /// Captures the SQL written by the action into an isolated buildable query scope,
    /// without affecting the segments accumulated on the outer builder.
    /// </summary>
    /// <param name="action">The delegate defining the query.</param>
    /// <returns>The captured SQL query.</returns>
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

        return new SqlQuery(scopedSegments);
    }

    /// <summary>
    /// Builds all accumulated segments into a result containing the rendered SQL string
    /// and the dictionary of extracted parameters.
    /// </summary>
    /// <param name="arguments">An optional anonymous object containing global template arguments.</param>
    /// <param name="clear">When <see langword="true"/> (default), <see cref="Clear"/> is called after building.</param>
    /// <returns>The query result ready for execution.</returns>
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
    /// Builds a previously captured query into a result.
    /// </summary>
    /// <param name="query">The isolated query to build.</param>
    /// <param name="arguments">An optional anonymous object containing global template arguments.</param>
    /// <returns>The query result ready for execution.</returns>
    public SqlQueryResult Build(ISqlQuery query, object? arguments = null) 
        => BuildSegments(query.Segments, arguments);

    private SqlQueryResult BuildSegments(IReadOnlyList<SqlSegment> segmentsToBuild, object? arguments)
    {
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

        var preprocessor = Context.Options.Preprocessor ?? SqlSegmentPreprocessor.Instance;
        var pipeline = new SqlPipeline(preprocessor, Context.Options.Rewriters);
        
        var compiledSegments = pipeline.Process(finalInputSegments, Context);

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
                throw new SqlDialectException(Context.Dialect.Kind.ToString(), featureName!);
            }
        }

        var vsb = new ValueStringBuilder(stackalloc char[2048]);

        try
        {
            for (int i = 0; i < compiledSegments.Count; i++)
            {
                CurrentRenderIndex = i;
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

    internal SqlSegment ProcessValue(object? value)
    {
        if (value is SqlSegment segment) 
            return segment;

        if (value is ISqlEntityBase entity) 
            return new SqlSegment(SqlSegmentType.Unresolved, entity);

        // Explicit structural fragments bypass parameterization
        if (value is ISqlFragment fragment) 
            return new SqlSegment(SqlSegmentType.Raw, fragment);

        return new SqlSegment(SqlSegmentType.Unresolved, value);
    }

    ISqlEntityBase<T> ISqlEntityRegistry.RegisterEntity<T>(string? name, string? schema, string? alias)
    {
        var entity = CreateEntity<T>(name, schema, alias);
        _entities.Add(entity);

        return entity;
    }

    internal ISqlEntityBase<T> CreateEntity<T>(string? name = null, string? schema = null, string? alias = null)
    {
        var meta = SqlMetadataRegistry.GetMetadata<T>();
        string physicalName = name ?? meta.Name;
        string? physicalSchema = schema ?? meta.Schema;

        if (meta.Type == SqlEntityType.View)
        {
            return new SqlView<T>(physicalName, physicalSchema, alias);
        }
        
        return new SqlTable<T>(physicalName, physicalSchema, alias);
    }
}