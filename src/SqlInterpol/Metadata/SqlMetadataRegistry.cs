using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reflection;
using SqlInterpol.Parsing;

namespace SqlInterpol.Metadata;

public static class SqlMetadataRegistry
{
    private static class Cache<T>
    {
        public static readonly SqlEntityMetadata Metadata = InitializeMetadata(typeof(T));
    }

    private static readonly ConcurrentDictionary<Type, SqlEntityMetadata> _runtimeCache = new();

    public static SqlEntityMetadata GetMetadata<T>() => Cache<T>.Metadata;

    public static SqlEntityMetadata GetMetadata(Type type)
    {
        return _runtimeCache.GetOrAdd(type, InitializeMetadata);
    }

    private static SqlEntityMetadata InitializeMetadata(Type type)
    {
        var entityAttr = type.GetCustomAttribute<SqlEntityAttribute>(inherit: true);
        string name = entityAttr?.Name ?? type.Name;
        string? schema = entityAttr?.Schema; 
        SqlEntityType entityType = entityAttr?.Type ?? SqlEntityType.Table;

        var columns = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .ToDictionary(
                p => (MemberInfo)p,
                p => p.GetCustomAttribute<SqlColumnAttribute>()?.Name ?? p.Name
            );

        return new SqlEntityMetadata(name, schema, entityType, columns);
    }

    public static string GetColumnName<T>(Expression<Func<T, object>> propertySelector)
    {
        var member = SqlExpressionHelper.GetMember(propertySelector);
        var meta = GetMetadata<T>();

        if (meta.Columns.TryGetValue(member, out var columnName))
        {
            return columnName;
        }

        throw new ArgumentException($"Property '{member.Name}' not found on {typeof(T).Name}");
    }

    public static string GetPropertyName<T>(Expression<Func<T, object>> propertySelector)
    {
        var member = GetMemberInfo(propertySelector);

        return member.Name;
    }

    private static MemberInfo GetMemberInfo(LambdaExpression expression)
    {
        Expression body = expression.Body;

        if (body is UnaryExpression unary && unary.NodeType == ExpressionType.Convert)
        {
            body = unary.Operand;
        }

        if (body is MemberExpression member)
        {
            return member.Member;
        }

        throw new ArgumentException("Expression must be a simple property access (e.g., x => x.Name).");
    }
}

public record SqlEntityMetadata(
    string Name, 
    string? Schema, 
    SqlEntityType Type,
    IReadOnlyDictionary<MemberInfo, string> Columns
);

public enum SqlEntityType { Table, View, Subquery }