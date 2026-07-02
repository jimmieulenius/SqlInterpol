using SqlInterpol.Parsing;

namespace SqlInterpol;

/// <summary>
/// A captured SQL query holding a stateless <see cref="SqlSegment"/> list ready for contextual rendering.
/// </summary>
public class SqlQuery(IReadOnlyList<SqlSegment> segments) : ISqlQuery
{
    public IReadOnlyList<SqlSegment> Segments { get; } = segments;
    
    /// <inheritdoc />
    public bool ExcludeParentheses { get; set; }

    /// <inheritdoc />
    public string ToSql(ISqlContext context, SqlRenderMode mode = SqlRenderMode.Default)
    {
        // 1. Compile the isolated segments using the provided outer context safely
        var preprocessor = context.Options.Preprocessor ?? SqlSegmentPreprocessor.Instance;
        var renderer = context.Options.Renderer ?? SqlSegmentRenderer.Instance;
        var pipeline = new SqlCompilationPipeline(preprocessor, context.Options.Rewriters);
        var compiledSegments = pipeline.Compile(Segments, context);

        // 2. Render to text
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
public class SqlQuery<T> : SqlEntityBase<T>, ISqlQuery<T>
{
    /// <inheritdoc />
    public IReadOnlyList<SqlSegment> Segments => _innerQuery.Segments;
    
    /// <inheritdoc />
    public bool ExcludeParentheses 
    { 
        get => _innerQuery.ExcludeParentheses; 
        set => _innerQuery.ExcludeParentheses = value; 
    }

    private readonly ISqlQuery _innerQuery;

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
        var pipeline = new SqlCompilationPipeline(preprocessor, context.Options.Rewriters);
        var compiledSegments = pipeline.Compile(Segments, context);

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