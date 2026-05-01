using System.Linq.Expressions;

namespace SqlInterpol;

public interface ISqlEntity : ISqlEntityProjection
{
    string Name { get; }
    string? Schema { get; }

    ISqlReference this[string columnName] { get; }

    ISqlFragment Column(string name);
    ISqlFragment Alias(string alias);
}

public interface ISqlEntity<T> : ISqlEntity, ISqlEntityProjection<T>
{
    ISqlReference this[Expression<Func<T, object>> propertySelector] { get; }
}