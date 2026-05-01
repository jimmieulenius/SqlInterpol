using SqlInterpol.Metadata;

namespace SqlInterpol;

public interface ISqlEntityProjection : ISqlProjection
{
    ISqlDeclaration Declaration { get; }
}

public interface ISqlEntityProjection<T> : ISqlEntityProjection, ISqlProjection<T>
{
}