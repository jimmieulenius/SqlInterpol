using System.Runtime.CompilerServices;
using SqlInterpol.Config;
using SqlInterpol.Metadata;
using SqlInterpol.Parsing;
using SqlInterpol.Rendering;

namespace SqlInterpol;

public partial class SqlBuilder : ISqlEntityRegistry
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

    private SqlBuilder Append(string? value)
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

    public SqlBuilder AppendLine() => Append(Environment.NewLine);

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
        // === EARLY BUILD-TIME VALIDATION (on original segments, before dialect rewriting erases evidence) ===
        var errors = new List<string>();
        foreach (var segment in segmentsToBuild)
        {
            SqlFeature? requiredFeature = null;
            string? featureName = null;

            // 1. Check Explicit AST Fragments
            if (segment.Value is ISqlFeatureRequirement req)
            {
                requiredFeature = req.RequiredFeature;
                featureName = req.FeatureName;
            }
            // 2. Check Raw WYSIWYG Strings tagged by the lexer
            else if (segment.Tag == SqlSegmentTag.ForUpdateKeyword)
            {
                requiredFeature = SqlFeature.ForUpdate;
                featureName = "FOR UPDATE";
            }
            else if (segment.Tag == SqlSegmentTag.ForShareKeyword)
            {
                requiredFeature = SqlFeature.ForShare;
                featureName = "FOR SHARE";
            }
            else if (segment.Tag == SqlSegmentTag.ReturningKeyword)
            {
                requiredFeature = SqlFeature.Returning;
                featureName = "RETURNING";
            }
            else if (segment.Tag == SqlSegmentTag.OnConflictKeyword)
            {
                requiredFeature = SqlFeature.OnConflict;
                featureName = "ON CONFLICT";
            }

            if (requiredFeature.HasValue && !Context.Dialect.SupportedFeatures.Contains(requiredFeature.Value))
            {
                // Add to aggregate list to show all errors at once!
                errors.Add($"'{featureName}' is not supported by {Context.Dialect.Kind}.");
            }
        }

        if (errors.Count > 0)
        {
            throw new SqlDialectException($"Dialect capabilities validation failed:{Environment.NewLine}- " + string.Join($"{Environment.NewLine}- ", errors));
        }

        var finalSegments = Context.Dialect.RewriteSegments(segmentsToBuild).ToList();

        // === RENDER PHASE ===
        var vsb = new ValueStringBuilder(stackalloc char[2048]);

        try
        {
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

    internal SqlSegment ProcessValue(object? value) => Parser.ProcessValue(Context, value);

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