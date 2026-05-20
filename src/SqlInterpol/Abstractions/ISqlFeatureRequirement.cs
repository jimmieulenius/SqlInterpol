
namespace SqlInterpol;

public interface ISqlFeatureRequirement
{
    SqlFeature RequiredFeature { get; }
    string FeatureName { get; }
}