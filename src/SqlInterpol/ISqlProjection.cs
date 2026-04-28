using SqlInterpol.Metadata;

namespace SqlInterpol;

public interface ISqlProjection : ISqlFragment
{
    ISqlDeclaration Declaration { get; }
    ISqlReference Reference { get; }
    ISqlProjection? Parent { get; }
    ISqlReference this[string columnName] { get; }
}