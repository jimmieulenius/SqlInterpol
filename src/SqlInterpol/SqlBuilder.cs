using System.Runtime.CompilerServices;
using SqlInterpol.Config;
using SqlInterpol.Parsing;
using SqlInterpol.Metadata;

namespace SqlInterpol;

public class SqlBuilder
{
    private readonly List<SqlSegment> _segments = [];
    
    public SqlContext Context { get; }

    public ISqlDialect Dialect => Context.Dialect;
    public Dictionary<string, object?> Parameters => Context.Parameters;

    public SqlBuilder(ISqlDialect dialect, SqlInterpolOptions? options = null)
    {
        options ??= SqlInterpolOptions.Default;
        options.Dialect = dialect.Kind; // Ensure the options know which dialect we're using
        Context = new SqlContext(dialect, options);
    }

    public SqlBuilder Append(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return this;
        }

        SqlParser.ProcessLiteral(Context, value.AsSpan());
        
        _segments.Add(new SqlSegment(SegmentType.Literal, value));

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

    public SqlQueryResult Build()
    {
        var vsb = new ValueStringBuilder(stackalloc char[1024]);

        try
        {
            foreach (var segment in _segments)
            {
                RenderSegment(ref vsb, segment);
            }

            return new SqlQueryResult(vsb.ToString(), Context.Parameters);
        }
        finally
        {
            vsb.Dispose();
        }
    }

    private void RenderSegment(ref ValueStringBuilder vsb, SqlSegment segment)
    {
        switch (segment.Type)
        {
            case SegmentType.Literal:
                vsb.Append((string)segment.Value!);
                break;
            case SegmentType.Projection:
                var proj = (ISqlProjection)segment.Value!;
                vsb.Append(segment.Context?.ExpectsDeclaration == true 
                    ? proj.Declaration.ToSql(Context) 
                    : proj.Reference.ToSql(Context));
                break;
            case SegmentType.Reference:
                vsb.Append(((ISqlReference)segment.Value!).ToSql(Context));
                break;
            case SegmentType.Parameter:
                vsb.Append(Context.Dialect.ParameterPrefix);
                vsb.Append((string)segment.Value!);
                break;
        }
    }

    internal SqlEntity<T> CreateEntity<T>()
    {
        var meta = SqlMetadataRegistry.GetMetadata<T>();
        
        // This is where you'd decide between SqlTable and SqlView.
        // For now, we default to the Table implementation.
        return new SqlTable<T>(meta.Name, meta.Schema);
    }
}