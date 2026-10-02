using System.ComponentModel;
using System.Runtime.CompilerServices;
using SqlInterpol.Configuration;
using SqlInterpol.Dialects;
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
    
    /// <summary>
    /// Indicates whether at least one Append call was successfully intercepted by the AOT source generator.
    /// </summary>
    public bool IsAotIntercepted { get; set; } = false;

    /// <summary>
    /// Remembers the AOT interception status of the most recently built query. 
    /// Exposed publicly for testing frameworks and custom dialect authors.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Advanced)]
    public bool LastBuildWasAotIntercepted { get; private set; }
    
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
        var finalOptions = SqlInterpolOptions.GetOptions(dialect, options);
        var renderer = finalOptions.Renderer ?? SqlSegmentRenderer.Instance;
        
        Context = new SqlContext(this, dialect, renderer, finalOptions);
        
        SqlMetadataRegistry.ActiveOptions.Value = finalOptions;
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
        IsAotIntercepted = false; // Reset the telemetry tracking flag
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
        var meta = SqlMetadataRegistry.GetMetadata<T>(Context.Options);
        
        string physicalName = name ?? meta.Name;
        string? physicalSchema = schema ?? meta.Schema;

        if (meta.Type == SqlEntityType.View)
        {
            return new SqlView<T>(physicalName, physicalSchema, alias);
        }
        
        return new SqlTable<T>(physicalName, physicalSchema, alias);
    }
}