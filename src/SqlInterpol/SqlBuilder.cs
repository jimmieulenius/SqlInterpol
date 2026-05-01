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
    
    private ISqlParser Parser => Context.Parser;

    public SqlBuilder(ISqlDialect dialect, SqlInterpolOptions? options = null)
    {
        var baseOptions = options ?? SqlInterpolOptions.GetDefault(dialect);
        var finalOptions = baseOptions with { Dialect = dialect.Kind };
        var parser = finalOptions.Parser ?? new DefaultSqlParser();   // ← resolved once here
        Context = new SqlContext(this, dialect, parser, finalOptions);
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
                RenderSegment(ref vsb, i, _segments[i]);
            }

            return new SqlQueryResult(vsb.ToString(), Context.Parameters);
        }
        finally
        {
            vsb.Dispose();
        }
    }

    private void RenderSegment(ref ValueStringBuilder vsb, int index, SqlSegment segment)
    {
        switch (segment.Type)
        {
            case SqlSegmentType.Projection:
            case SqlSegmentType.Reference:
                if (segment.Value is ISqlFragment fragment)
                {
                    var mode = ResolveRenderMode(index, segment);
                    vsb.Append(fragment.ToSql(Context, mode));
                }
                break;

            case SqlSegmentType.Literal:
            case SqlSegmentType.Parameter:
                vsb.Append(segment.Value?.ToString() ?? string.Empty);
                break;

            case SqlSegmentType.Raw:
                if (segment.Value is ISqlFragment rawFrag)
                    vsb.Append(rawFrag.ToSql(Context, SqlRenderMode.Default));
                else
                    vsb.Append(segment.Value?.ToString() ?? string.Empty);
                break;
        }
    }

    private SqlRenderMode ResolveRenderMode(int index, SqlSegment segment)
    {
        if (segment.IsAliasTarget)
        {
            return SqlRenderMode.AliasOnly;
        }

        if (segment.Value is not ISqlEntity entity)
        {
            return SqlRenderMode.Default;
        }

        if (index + 1 < _segments.Count)
        {
            var next = _segments[index + 1];

            if (next.Type == SqlSegmentType.Literal)
            {
                var text = next.Value?.ToString()?.TrimStart();

                if (text?.StartsWith("AS ", StringComparison.OrdinalIgnoreCase) == true
                    || text?.StartsWith("AS\n", StringComparison.OrdinalIgnoreCase) == true)
                {
                    return SqlRenderMode.BaseName;
                }
            }
            else if (next.Type == SqlSegmentType.Raw)
            {
                return SqlRenderMode.BaseName;
            }
        }

        return !string.IsNullOrEmpty(entity.Reference.Alias)
            ? SqlRenderMode.Declaration
            : SqlRenderMode.BaseName;
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