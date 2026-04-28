using System.Linq.Expressions;
using SqlInterpol.Metadata;

namespace SqlInterpol;

public interface ISqlProjection : ISqlFragment
{
    ISqlDeclaration Declaration { get; }
    ISqlReference Reference { get; }
    ISqlProjection? Parent { get; }
    ISqlReference this[string columnName] { get; }
}

public interface ISqlProjection<T> : ISqlProjection
{
    // This allows us to keep the indexer logic available 
    // on anything that claims to project the shape of T
    ISqlReference this[Expression<Func<T, object>> propertySelector] { get; }
}