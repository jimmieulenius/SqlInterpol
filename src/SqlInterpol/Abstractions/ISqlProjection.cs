using SqlInterpol.Models;

namespace SqlInterpol.Abstractions;

public interface ISqlProjection : ISqlFragment
{
    ISqlReference GetColumnReference(string propertyName, SqlInterpolOptions options);
    ISqlDeclaration Declaration { get; }
    ISqlReference Reference { get; }
    ISqlProjection? Parent { get; }
}