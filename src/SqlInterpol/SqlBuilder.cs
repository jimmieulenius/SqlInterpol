using System.Text;
using System.Runtime.CompilerServices;
using SqlInterpol.Config;
using SqlInterpol.Parsing;
using SqlInterpol.Metadata;
using SqlInterpol.Dialects;

namespace SqlInterpol;

public class SqlBuilder
{
    // private readonly StringBuilder _sql = new();
    private readonly List<SqlSegment> _allSegments = new();
    
    public SqlContext Context { get; }

    // Shortcuts for easier access to context data
    public ISqlDialect Dialect => Context.Dialect;
    public Dictionary<string, object?> Parameters => Context.Parameters;

    public SqlBuilder(ISqlDialect dialect, SqlInterpolOptions? options = null)
    {
        options ??= SqlInterpolOptions.Default;
        options.Dialect = dialect.Kind; // Ensure the options know which dialect we're using
        Context = new SqlContext(dialect, options);
    }

    public static SqlBuilder MySql(SqlInterpolOptions? opt = null) 
        => new(new MySqlSqlDialect(), opt);

    public static SqlBuilder Oracle(SqlInterpolOptions? opt = null) 
        => new(new OracleSqlDialect(), opt);

    public static SqlBuilder PostgreSql(SqlInterpolOptions? opt = null) 
        => new(new PostgreSqlSqlDialect(), opt);

    public static SqlBuilder SqLite(SqlInterpolOptions? opt = null) 
        => new(new SqLiteSqlDialect(), opt);

    public static SqlBuilder SqlServer(SqlInterpolOptions? opt = null) 
        => new(new SqlServerSqlDialect(), opt);

    public static SqlBuilder ForDialect<T>(SqlInterpolOptions? opt = null) where T : ISqlDialect, new()
        => new(new T(), opt);

    public SqlBuilder Append(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return this;
        }

        SqlParser.ProcessLiteral(Context, value.AsSpan());
        
        _allSegments.Add(new SqlSegment(SegmentType.Literal, value));

        return this;
    }

    public SqlBuilder Append([InterpolatedStringHandlerArgument("")] ref SqlQueryInterpolatedStringHandler handler)
    {
        handler.TransferSegments(_allSegments);

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

    // public SqlBuilder Append([InterpolatedStringHandlerArgument("")] ref SqlQueryInterpolatedStringHandler handler)
    // {
    //     handler.TransferSegments(_allSegments);

    //     return this;
    // }

    // Interpolated Version (The magic happens here)
    // public SqlBuilder Append([InterpolatedStringHandlerArgument("")] ref SqlQueryInterpolatedStringHandler handler)
    // {
    //     _sql.Append(handler.GetBuiltSql());
    //     return this;
    // }

    // public SqlBuilder AppendLine([InterpolatedStringHandlerArgument("")] ref SqlQueryInterpolatedStringHandler handler)
    // {
    //     _sql.Append(handler.GetBuiltSql());
    //     _sql.AppendLine();
    //     return this;
    // }

    // Raw String Version (For comments or non-parameterized SQL)
    // public SqlBuilder Append(string rawSql)
    // {
    //     _sql.Append(rawSql);
    //     return this;
    // }

    // public SqlBuilder AppendLine(string rawSql)
    // {
    //     _sql.AppendLine(rawSql);
    //     return this;
    // }

    public SqlQueryResult Build()
    {
        var vsb = new ValueStringBuilder(stackalloc char[1024]);

        try
        {
            foreach (var segment in _allSegments)
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
        var meta = Metadata.SqlMetadataRegistry.GetMetadata<T>();
        
        // This is where you'd decide between SqlTable and SqlView.
        // For now, we default to the Table implementation.
        return new SqlTable<T>(meta.Name, meta.Schema);
    }
}