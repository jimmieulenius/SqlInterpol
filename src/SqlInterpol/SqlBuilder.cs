using System.Runtime.CompilerServices;
using SqlInterpol.Config;
using SqlInterpol.Metadata;
using SqlInterpol.Parsing;
using SqlInterpol.Rendering;

namespace SqlInterpol;

public class SqlBuilder : ISqlEntityRegistry
{
    private List<SqlSegment> _segments = [];
    private readonly List<ISqlEntity> _entities = [];
    public SqlContext Context { get; }
    private ISqlInterpolationParser Parser => Context.Parser;
    private ISqlSegmentRenderer Renderer => Context.Renderer;

    public SqlBuilder(ISqlDialect dialect, SqlInterpolOptions? options = null)
    {
        var baseOptions = options ?? SqlInterpolOptions.GetDefault(dialect);
        var finalOptions = baseOptions with { Dialect = dialect.Kind };
        var parser = finalOptions.Parser ?? SqlInterpolationParser.Instance;
        var renderer = options?.Renderer ?? SqlSegmentRenderer.Instance;
        Context = new SqlContext(this, dialect, parser, renderer, finalOptions);
    }

    public SqlBuilder Append(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return this;
        }

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

    public SqlQueryResult Build() => BuildSegments(_segments);

    public SqlQueryResult Build(ISqlQuery query) => BuildSegments(query.Segments);

    private SqlQueryResult BuildSegments(IReadOnlyList<SqlSegment> segmentsToBuild)
    {
        // 1. AST Rewriting Phase: Let the active dialect intercept and modify the segments!
        var finalSegments = Context.Dialect.RewriteSegments(segmentsToBuild).ToList();
        var vsb = new ValueStringBuilder(stackalloc char[2048]);

        try
        {
            // 2. Rendering Phase: Loop through the REWRITTEN segments
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

    // internal SqlSegment ProcessLiteral(string value)
    // {
    //     Parser.ProcessLiteral(Context, value.AsSpan());

    //     return new SqlSegment(SqlSegmentType.Literal, value);
    // }

    internal SqlSegment ProcessValue(object? value)
    {
        return Parser.ProcessValue(Context, value);
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