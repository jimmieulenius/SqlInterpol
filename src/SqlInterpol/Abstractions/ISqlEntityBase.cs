using System.Linq.Expressions;

namespace SqlInterpol;

public interface ISqlEntityBase : ISqlFragment
{
    ISqlDeclaration Declaration { get; }
    ISqlReference Reference { get; }
    
    ISqlReference this[string columnName] { get; }

    ISqlFragment Column(string name);
    // ISqlFragment Alias(string alias);
}

public interface ISqlEntityBase<T> : ISqlEntityBase
{
    ISqlReference this[Expression<Func<T, object>> propertySelector] { get; }
}