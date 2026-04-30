using System.Runtime.CompilerServices;
using SqlInterpol.Config;
using SqlInterpol.Metadata;
using SqlInterpol.Parsing;

namespace SqlInterpol;

public class SqlBuilder
{
    private readonly List<SqlSegment> _segments = [];
    public SqlContext Context { get; }
    
    // The parser is now retrieved from the context/options
    private ISqlParser Parser => SqlParser.Instance;

    public SqlBuilder(ISqlDialect dialect, SqlInterpolOptions? options = null)
    {
        var baseOptions = options ?? SqlInterpolOptions.GetDefault(dialect);
        var finalOptions = baseOptions with { Dialect = dialect.Kind };
        Context = new SqlContext(dialect, finalOptions);
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

    private void RenderSegment(ref ValueStringBuilder vsb, SqlSegment segment)
    {
        switch (segment.Type)
        {
            case SqlSegmentType.Literal:
                vsb.Append((string)segment.Value!);
                break;

            case SqlSegmentType.Projection:
                var proj = (ISqlProjection)segment.Value!;
                
                if (segment.RenderMode == SqlRenderMode.AliasOnly)
                {
                    vsb.Append(Context.Dialect.OpenQuote);
                    vsb.Append(proj.PropertyName);
                    vsb.Append(Context.Dialect.CloseQuote);
                }
                else
                {
                    // Call ToSql on the Reference or Declaration
                    vsb.Append(segment.Keyword?.ExpectsDeclaration == true 
                        ? proj.Declaration.ToSql(Context) 
                        : proj.Reference.ToSql(Context));
                }
                break;

            case SqlSegmentType.Reference:
                vsb.Append(((ISqlReference)segment.Value!).ToSql(Context));
                break;

            case SqlSegmentType.Parameter:
                vsb.Append((string)segment.Value!);
                break;

            case SqlSegmentType.Raw:
                // Check if it's a fragment (p.Column("X")) or just a raw string
                if (segment.Value is ISqlFragment frag)
                {
                    vsb.Append(frag.ToSql(Context));
                }
                else
                {
                    vsb.Append(segment.Value?.ToString() ?? string.Empty);
                }
                break;
        }
    }

    internal ISqlEntity<T> CreateEntity<T>()
    {
        var meta = SqlMetadataRegistry.GetMetadata<T>();
        
        // The Factory Switch
        return meta.Type switch
        {
            SqlEntityType.View => new SqlView<T>(meta.Name, meta.Schema),
            
            // Default to Table for SqlEntityType.Table or if no attribute is present
            _ => new SqlTable<T>(meta.Name, meta.Schema)
        };
    }
}