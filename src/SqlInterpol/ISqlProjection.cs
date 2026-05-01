using SqlInterpol.Metadata;

namespace SqlInterpol;

public interface ISqlProjection : ISqlFragment
{
    ISqlDeclaration Declaration { get; }
    ISqlReference Reference { get; }
    string PropertyName { get; }
}

public interface ISqlProjection<T> : ISqlProjection
{
}