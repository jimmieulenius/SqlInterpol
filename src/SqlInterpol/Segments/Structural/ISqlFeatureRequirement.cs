using SqlInterpol.Configuration;

namespace SqlInterpol.Segments;

/// <summary>
/// Marks a SQL fragment as requiring a specific dialect feature, enabling pre-render validation.
/// </summary>
/// <remarks>
/// During <see cref="SqlBuilder.Build(bool)"/>, all segments are inspected for <see cref="ISqlFeatureRequirement"/>.
/// If any required feature is absent from the active dialect's <see cref="ISqlDialect.SupportedFeatures"/>,
/// a <see cref="SqlDialectException"/> is thrown listing all violations before any SQL is rendered.
/// </remarks>
public interface ISqlFeatureRequirement
{
    /// <summary>Gets the dialect feature this fragment requires.</summary>
    SqlFeature RequiredFeature { get; }

    /// <summary>Gets the human-readable name of the required feature, used in error messages.</summary>
    string FeatureName { get; }
}