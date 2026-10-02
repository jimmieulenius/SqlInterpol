using SqlInterpol.Configuration;
using SqlInterpol.Dialects;
using SqlInterpol.Segments;

namespace SqlInterpol.Pipeline;

/// <summary>
/// Ensures compiled segments only use features the active dialect supports.
/// Owns the tag→feature map so <see cref="SqlBuilder"/> does not duplicate gate logic.
/// </summary>
internal static class SqlFeatureGate
{
    /// <summary>
    /// Maps well-known segment tags to their required <see cref="SqlFeature"/> and human-readable
    /// feature name. Adding a new feature only requires a new entry here.
    /// </summary>
    private static readonly IReadOnlyDictionary<string, (SqlFeature Feature, string Name)> TagFeatureMap =
        new Dictionary<string, (SqlFeature, string)>(StringComparer.OrdinalIgnoreCase)
        {
            [SqlSegmentTag.ForUpdateKeyword] = (SqlFeature.ForUpdate, "FOR UPDATE"),
            [SqlSegmentTag.ForShareKeyword] = (SqlFeature.ForShare, "FOR SHARE"),
            [SqlSegmentTag.ReturningKeyword] = (SqlFeature.Returning, "RETURNING"),
            [SqlSegmentTag.OnConflictKeyword] = (SqlFeature.OnConflict, "ON CONFLICT"),
            [SqlSegmentTag.DeleteAsKeyword] = (SqlFeature.DeleteAs, "DELETE with target alias"),
            [SqlSegmentTag.UpdateAsKeyword] = (SqlFeature.UpdateAs, "UPDATE with target alias"),
        };

    /// <summary>
    /// Throws <see cref="SqlDialectException"/> when any compiled segment requires an unsupported feature.
    /// </summary>
    public static void EnsureSupported(ISqlDialect dialect, IReadOnlyList<SqlSegment> compiledSegments)
    {
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
                    if (TagFeatureMap.TryGetValue(segment.Tags[t], out var mapped))
                    {
                        requiredFeature = mapped.Feature;
                        featureName = mapped.Name;
                        break;
                    }
                }
            }

            if (requiredFeature.HasValue && !dialect.SupportedFeatures.Contains(requiredFeature.Value))
            {
                throw new SqlDialectException(dialect.Kind.ToString(), featureName!);
            }
        }
    }
}
