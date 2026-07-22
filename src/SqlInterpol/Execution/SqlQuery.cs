using SqlInterpol.Configuration;
using SqlInterpol.Pipeline;
using SqlInterpol.Schema;
using SqlInterpol.Segments;

namespace SqlInterpol.Execution;

/// <summary>
/// A captured SQL query holding a stateless segment list ready for contextual rendering.
/// </summary>
/// <param name="segments">The sequence of parsed segments forming this query.</param>
public class SqlQuery(IReadOnlyList<SqlSegment> segments) : ISqlQuery
{
    /// <inheritdoc />
    public IReadOnlyList<SqlSegment> Segments { get; } = segments;
    
    /// <inheritdoc />
    public bool ExcludeParentheses { get; set; }

    /// <inheritdoc />
    public string ToSql(ISqlContext context, SqlRenderMode mode = SqlRenderMode.Default)
    {
        var preprocessor = context.Options.Preprocessor ?? SqlSegmentPreprocessor.Instance;
        var renderer = context.Options.Renderer ?? SqlSegmentRenderer.Instance;
        var pipeline = new SqlPipeline(preprocessor, context.Options.Rewriters);
        
        var compiledSegments = pipeline.Process(Segments, context);
        var vsb = new ValueStringBuilder(stackalloc char[2048]);
        
        try
        {
            for (int i = 0; i < compiledSegments.Count; i++)
            {
                vsb.Append(renderer.Render(context, compiledSegments[i], i, compiledSegments) ?? string.Empty);
            }
            
            var sql = vsb.ToString();
            
            if (ExcludeParentheses) return sql;
            return $"({sql})";
        }
        finally
        {
            vsb.Dispose();
        }
    }
}

/// <summary>
/// A typed SQL subquery scope bound to a primary entity model type <typeparamref name="T"/>.
/// </summary>
/// <typeparam name="T">The model type representing the query projection.</typeparam>
public class SqlQuery<T> : SqlEntityBase<T>, ISqlQuery<T>
{
    private readonly ISqlQuery _innerQuery;

    /// <inheritdoc />
    public IReadOnlyList<SqlSegment> Segments => _innerQuery.Segments;
    
    /// <inheritdoc />
    public bool ExcludeParentheses 
    { 
        get => _innerQuery.ExcludeParentheses; 
        set => _innerQuery.ExcludeParentheses = value; 
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="SqlQuery{T}"/> class.
    /// </summary>
    /// <param name="innerQuery">The captured inner query to wrap.</param>
    /// <param name="alias">The explicit alias to map this subquery to.</param>
    public SqlQuery(ISqlQuery innerQuery, string? alias)
    {
        _innerQuery = innerQuery;
        Reference = new SqlEntityReference(this)
        {
            Alias = alias,
            FallbackAlias = typeof(T).Name,
            IsAliasQuoted = !string.IsNullOrWhiteSpace(alias)
        };
        Declaration = new SqlDeclaration(this);
    }

    /// <inheritdoc />
    public override string ToSql(ISqlContext context, SqlRenderMode mode = SqlRenderMode.Default)
    {
        var aliasToUse = Reference.Alias ?? Reference.FallbackAlias;
        var escapedAlias = Reference.IsAliasQuoted ? context.Dialect.QuoteIdentifier(aliasToUse) : aliasToUse;
        
        if (mode == SqlRenderMode.AliasOnly) return escapedAlias;
        if (mode == SqlRenderMode.AsAlias) return context.Dialect.ApplyAlias("", escapedAlias).Trim();
        
        var preprocessor = context.Options.Preprocessor ?? SqlSegmentPreprocessor.Instance;
        var renderer = context.Options.Renderer ?? SqlSegmentRenderer.Instance;
        var pipeline = new SqlPipeline(preprocessor, context.Options.Rewriters);
        
        var compiledSegments = pipeline.Process(Segments, context);
        var vsb = new ValueStringBuilder(stackalloc char[2048]);
        
        try
        {
            for (int i = 0; i < compiledSegments.Count; i++)
            {
                vsb.Append(renderer.Render(context, compiledSegments[i], i, compiledSegments) ?? string.Empty);
            }
            
            var innerSql = vsb.ToString();
            
            if (ExcludeParentheses) return innerSql;
            
            return mode switch
            {
                SqlRenderMode.Declaration => context.Dialect.ApplyAlias($"({innerSql})", escapedAlias),
                SqlRenderMode.BaseName => $"({innerSql})",
                _ => $"({innerSql})"
            };
        }
        finally
        {
            vsb.Dispose();
        }
    }
}