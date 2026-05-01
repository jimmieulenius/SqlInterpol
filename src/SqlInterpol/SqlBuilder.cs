using System.Runtime.CompilerServices;
using SqlInterpol.Config;
using SqlInterpol.Metadata;
using SqlInterpol.Parsing;

namespace SqlInterpol;

public class SqlBuilder : ISqlEntityRegistry
{
    private readonly List<SqlSegment> _segments = [];
    private readonly List<ISqlEntity> _entities = [];
    public SqlContext Context { get; }
    public SqlSegment? LastSegment => _segments.Count > 0 ? _segments[^1] : null;
    
    // The parser is now retrieved from the context/options
    private ISqlParser Parser => SqlParser.Instance;

    public SqlBuilder(ISqlDialect dialect, SqlInterpolOptions? options = null)
    {
        var baseOptions = options ?? SqlInterpolOptions.GetDefault(dialect);
        var finalOptions = baseOptions with { Dialect = dialect.Kind };
        Context = new SqlContext(this, dialect, finalOptions);
    }

    public SqlBuilder Append(string? value)
    {
        if (string.IsNullOrEmpty(value)) return this;
        _segments.Add(ProcessLiteral(value));
        return this;
    }

    public SqlBuilder Append([InterpolatedStringHandlerArgument("")] ref SqlQueryInterpolatedStringHandler handler)
    {
        handler.TransferSegments(_segments);
        return this;
    }

    public SqlBuilder AppendLine() 
        => Append(Environment.NewLine);

    public SqlBuilder AppendLine(string? value) 
        => Append(value).AppendLine();

    public SqlBuilder AppendLine([InterpolatedStringHandlerArgument("")] ref SqlQueryInterpolatedStringHandler handler)
    {
        Append(ref handler);
        return AppendLine();
    }

    internal SqlSegment ProcessLiteral(string value)
    {
        // One call to the brain
        Parser.ProcessLiteral(Context, value.AsSpan());
        return new SqlSegment(SqlSegmentType.Literal, value);
    }

    internal SqlSegment ProcessValue(object? value)
    {
        // One call to the brain
        return Parser.ProcessValue(Context, value);
    }

    public SqlQueryResult Build()
    {
        var vsb = new ValueStringBuilder(stackalloc char[2048]);
        try
        {
            foreach (var segment in _segments) RenderSegment(ref vsb, segment);
            return new SqlQueryResult(vsb.ToString(), Context.Parameters);
        }
        finally { vsb.Dispose(); }
    }

    ISqlEntity<T> ISqlEntityRegistry.RegisterEntity<T>(string? name, string? schema)
    {
        var entity = CreateEntity<T>(name, schema);

        _entities.Add(entity); // Keep for query-wide validation/processing

        return entity;
    }

    private void RenderSegment(ref ValueStringBuilder vsb, SqlSegment segment)
    {
        switch (segment.Type)
        {
            case SqlSegmentType.Projection:
            case SqlSegmentType.Reference:
                if (segment.Value is ISqlFragment fragment)
                {
                    // CRITICAL: We call ToSql with the RenderMode.
                    // This is where SqlRenderMode.BaseName finally gets to work!
                    vsb.Append(fragment.ToSql(Context, segment.RenderMode));
                }
                break;

            case SqlSegmentType.Literal:
            case SqlSegmentType.Parameter:
                vsb.Append(segment.Value?.ToString() ?? string.Empty);
                break;

            case SqlSegmentType.Raw:
                if (segment.Value is ISqlFragment rawFrag)
                    vsb.Append(rawFrag.ToSql(Context, segment.RenderMode));
                else
                    vsb.Append(segment.Value?.ToString() ?? string.Empty);
                break;
        }
    }

    internal ISqlEntity<T> CreateEntity<T>(string? name = null, string? schema = null)
    {
        var meta = SqlMetadataRegistry.GetMetadata<T>();
        
        // The Factory Switch
        return meta.Type switch
        {
            SqlEntityType.View => new SqlView<T>( name ?? meta.Name, schema ?? meta.Schema),
            
            // Default to Table for SqlEntityType.Table or if no attribute is present
            _ => new SqlTable<T>(name ?? meta.Name, schema ?? meta.Schema)
        };
    }
}