using System.Diagnostics;
using System.Runtime.CompilerServices;
using SqlInterpol.Configuration;
using SqlInterpol.Dialects;
using SqlInterpol.Execution;
using SqlInterpol.Pipeline;
using SqlInterpol.Schema;
using SqlInterpol.Segments;

namespace SqlInterpol;

public partial class SqlBuilder
{
    /// <summary>
    /// Maps well-known segment tags to their required <see cref="SqlFeature"/> and human-readable
    /// feature name. Adding a new feature only requires a new entry here — no changes to the
    /// validation loop in <see cref="BuildSegments"/>.
    /// </summary>
    private static readonly IReadOnlyDictionary<string, (SqlFeature Feature, string Name)> _tagFeatureMap =
        new Dictionary<string, (SqlFeature, string)>(StringComparer.OrdinalIgnoreCase)
        {
            [SqlSegmentTag.ForUpdateKeyword]  = (SqlFeature.ForUpdate,  "FOR UPDATE"),
            [SqlSegmentTag.ForShareKeyword]   = (SqlFeature.ForShare,   "FOR SHARE"),
            [SqlSegmentTag.ReturningKeyword]  = (SqlFeature.Returning,  "RETURNING"),
            [SqlSegmentTag.OnConflictKeyword] = (SqlFeature.OnConflict, "ON CONFLICT"),
            [SqlSegmentTag.DeleteAsKeyword]   = (SqlFeature.DeleteAs,   "DELETE with target alias"),
            [SqlSegmentTag.UpdateAsKeyword]   = (SqlFeature.UpdateAs,   "UPDATE with target alias"),
        };

    /// <summary>
    /// Builds all accumulated segments into a result containing the rendered SQL string
    /// and the dictionary of extracted parameters.
    /// </summary>
    /// <param name="arguments">An optional anonymous object containing global template arguments.</param>
    /// <param name="clear">When <see langword="true"/> (default), <see cref="Clear"/> is called after building.</param>
    /// <param name="callerMemberName">Automatically populated by the compiler for telemetry tracking.</param>
    /// <param name="callerFilePath">Automatically populated by the compiler for telemetry tracking.</param>
    /// <param name="callerLineNumber">Automatically populated by the compiler for telemetry tracking.</param>
    /// <returns>The query result ready for execution.</returns>
    public SqlQueryResult Build(
        object? arguments = null, 
        bool clear = true,
        [CallerMemberName] string callerMemberName = "",
        [CallerFilePath] string callerFilePath = "",
        [CallerLineNumber] int callerLineNumber = 0)
    {
        try
        {
            var options = Context.Options;

            // Fast path: No telemetry requested
            if (options.OnQueryBuilt == null)
            {
                return BuildSegments(_segments, arguments);
            }

            // Telemetry path
            var sw = Stopwatch.StartNew();
            var result = BuildSegments(_segments, arguments);
            sw.Stop();

            options.OnQueryBuilt.Invoke(new SqlQueryTelemetry(
                CallerMemberName: callerMemberName,
                FilePath: callerFilePath,
                LineNumber: callerLineNumber,
                WasAotIntercepted: IsAotIntercepted,
                ParameterCount: result.Parameters.Count,
                BuildDuration: sw.Elapsed
            ));

            return result;
        }
        finally
        {
            // ALWAYS capture the AOT state and clean up, even if dialect validation throws!
            LastBuildWasAotIntercepted = IsAotIntercepted;
            
            if (clear)
            {
                Clear();
            }
        }
    }

    /// <summary>
    /// Builds a previously captured query into a result.
    /// </summary>
    /// <param name="query">The isolated query to build.</param>
    /// <param name="arguments">An optional anonymous object containing global template arguments.</param>
    /// <param name="callerMemberName">Automatically populated by the compiler for telemetry tracking.</param>
    /// <param name="callerFilePath">Automatically populated by the compiler for telemetry tracking.</param>
    /// <param name="callerLineNumber">Automatically populated by the compiler for telemetry tracking.</param>
    /// <returns>The query result ready for execution.</returns>
    public SqlQueryResult Build(
        ISqlQuery query, 
        object? arguments = null,
        [CallerMemberName] string callerMemberName = "",
        [CallerFilePath] string callerFilePath = "",
        [CallerLineNumber] int callerLineNumber = 0)
    {
        try
        {
            var options = Context.Options;

            // Fast path: No telemetry requested
            if (options.OnQueryBuilt == null)
            {
                return BuildSegments(query.Segments, arguments);
            }

            // Telemetry path
            var sw = Stopwatch.StartNew();
            var result = BuildSegments(query.Segments, arguments);
            sw.Stop();

            options.OnQueryBuilt.Invoke(new SqlQueryTelemetry(
                CallerMemberName: callerMemberName,
                FilePath: callerFilePath,
                LineNumber: callerLineNumber,
                WasAotIntercepted: IsAotIntercepted,
                ParameterCount: result.Parameters.Count,
                BuildDuration: sw.Elapsed
            ));

            return result;
        }
        finally
        {
            // ALWAYS capture the AOT state, even if dialect validation throws!
            LastBuildWasAotIntercepted = IsAotIntercepted;
        }
    }

    private SqlQueryResult BuildSegments(IReadOnlyList<SqlSegment> segmentsToBuild, object? arguments)
    {
        var previousOptions = SqlMetadataRegistry.ActiveOptions.Value;
        SqlMetadataRegistry.ActiveOptions.Value = Context.Options;

        try
        {
            List<SqlSegment>? resolvedSegments = null;
            IReadOnlyDictionary<string, Func<object, object?>>? getters = null;

            if (arguments != null)
            {
                getters = SqlMetadataRegistry.GetArgumentGetters(arguments.GetType());
            }

            for (int i = 0; i < segmentsToBuild.Count; i++)
            {
                var segment = segmentsToBuild[i];
                
                if (segment.Type == SqlSegmentType.Raw && segment.Value is SqlArgumentFragment argFragment)
                {
                    resolvedSegments ??= [.. segmentsToBuild];

                    string argName = argFragment.Name;
                    bool resolved = false;

                    if (getters != null && getters.TryGetValue(argName, out var getter))
                    {
                        object? val = getter(arguments!);
                        resolvedSegments[i] = new SqlSegment(SqlSegmentType.Unresolved, val);
                        resolved = true;
                    }

                    if (!resolved)
                    {
                        throw new ArgumentException(
                            $"The SQL template requires an argument named '{argName}', but it was not provided globally or locally.");
                    }
                }
            }

            var finalInputSegments = resolvedSegments != null ? (IReadOnlyList<SqlSegment>)resolvedSegments : segmentsToBuild;

            var preprocessor = Context.Options.Preprocessor ?? SqlSegmentPreprocessor.Instance;
            var pipeline = new SqlPipeline(preprocessor, Context.Options.Rewriters);
            
            var compiledSegments = pipeline.Process(finalInputSegments, Context);

            foreach (var segment in compiledSegments)
            {
                SqlFeature? requiredFeature = null;
                string? featureName = null;

                if (segment.Value is ISqlFeatureRequirement req)
                {
                    requiredFeature = req.RequiredFeature;
                    featureName = req.FeatureName;
                }
                else if (segment.Tags != null)
                {
                    for (int t = 0; t < segment.Tags.Length; t++)
                    {
                        if (_tagFeatureMap.TryGetValue(segment.Tags[t], out var mapped))
                        {
                            requiredFeature = mapped.Feature;
                            featureName = mapped.Name;
                            break;
                        }
                    }
                }

                if (requiredFeature.HasValue && !Context.Dialect.SupportedFeatures.Contains(requiredFeature.Value))
                {
                    throw new SqlDialectException(Context.Dialect.Kind.ToString(), featureName!);
                }
            }

            var vsb = new ValueStringBuilder(stackalloc char[2048]);

            try
            {
                for (int i = 0; i < compiledSegments.Count; i++)
                {
                    CurrentRenderIndex = i;
                    vsb.Append(Renderer.Render(Context, compiledSegments[i], i, compiledSegments) ?? string.Empty);
                }
                return new SqlQueryResult(vsb.ToString(), Context.Parameters.AsReadOnly());
            }
            finally
            {
                vsb.Dispose();
            }
        }
        finally
        {
            // Restore context to prevent cross-thread test pollution
            SqlMetadataRegistry.ActiveOptions.Value = previousOptions;
        }
    }
}
