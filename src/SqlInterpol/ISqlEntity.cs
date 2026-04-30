using System.Linq.Expressions;

namespace SqlInterpol;

public interface ISqlEntity : ISqlProjection
{
    string Name { get; }
    string? Schema { get; }

    ISqlReference this[string columnName] { get; }

    ISqlFragment Column(string name);
    ISqlFragment Alias(string alias);
}

public interface ISqlEntity<T> : ISqlEntity, ISqlProjection<T>
{
    ISqlReference this[Expression<Func<T, object>> propertySelector] { get; }
}