using System.Runtime.CompilerServices;
using SqlInterpol.Config;
using SqlInterpol.Metadata;
using SqlInterpol.Parsing;
using SqlInterpol.Rendering;

namespace SqlInterpol;

public class SqlBuilder : ISqlEntityRegistry
{
    private readonly List<SqlSegment> _segments = [];
    private readonly List<ISqlEntity> _entities = [];
    public SqlContext Context { get; }
    private ISqlParser Parser => Context.Parser;
    private ISqlSegmentRenderer Renderer => Context.Renderer;

    public SqlBuilder(ISqlDialect dialect, SqlInterpolOptions? options = null)
    {
        var baseOptions = options ?? SqlInterpolOptions.GetDefault(dialect);
        var finalOptions = baseOptions with { Dialect = dialect.Kind };
        var parser = finalOptions.Parser ?? new DefaultSqlParser();
        var renderer = options?.Renderer ?? DefaultSqlSegmentRenderer.Instance;
        Context = new SqlContext(this, dialect, parser, renderer, finalOptions);
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
        Parser.ProcessLiteral(Context, value.AsSpan());

        return new SqlSegment(SqlSegmentType.Literal, value);
    }

    internal SqlSegment ProcessValue(object? value)
    {
        return Parser.ProcessValue(Context, value);
    }

    public SqlQueryResult Build()
    {
        var vsb = new ValueStringBuilder(stackalloc char[2048]);

        try
        {
            for (int i = 0; i < _segments.Count; i++)
            {
                vsb.Append(Renderer.Render(Context, _segments[i], i, _segments) ?? string.Empty);
            }

            return new SqlQueryResult(vsb.ToString(), Context.Parameters);
        }
        finally
        {
            vsb.Dispose();
        }
    }

    ISqlEntity<T> ISqlEntityRegistry.RegisterEntity<T>(string? name, string? schema)
    {
        var entity = CreateEntity<T>(name, schema);

        _entities.Add(entity);

        return entity;
    }

    internal ISqlEntity<T> CreateEntity<T>(string? name = null, string? schema = null)
    {
        var meta = SqlMetadataRegistry.GetMetadata<T>();
        
        return meta.Type switch
        {
            SqlEntityType.View => new SqlView<T>( name ?? meta.Name, schema ?? meta.Schema),
            
            _ => new SqlTable<T>(name ?? meta.Name, schema ?? meta.Schema)
        };
    }
}