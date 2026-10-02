using SqlInterpol.Configuration;

namespace SqlInterpol.Segments;

/// <summary>
/// Marks a SQL fragment as requiring a specific dialect feature, enabling pre-render validation.
/// </summary>
/// <remarks>
/// During build, <see cref="SqlInterpol.Pipeline.SqlFeatureGate"/> inspects segments for
/// <see cref="ISqlFeatureRequirement"/> (and well-known tags). If any required feature is absent from
/// the active dialect's <see cref="ISqlDialect.SupportedFeatures"/>, a <see cref="SqlDialectException"/>
/// is thrown before any SQL is rendered.
/// </remarks>
public interface ISqlFeatureRequirement
{
    /// <summary>Gets the dialect feature this fragment requires.</summary>
    SqlFeature RequiredFeature { get; }

    /// <summary>Gets the human-readable name of the required feature, used in error messages.</summary>
    string FeatureName { get; }
}