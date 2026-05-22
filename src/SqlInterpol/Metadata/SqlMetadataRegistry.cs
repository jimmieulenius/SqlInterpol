using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reflection;
using SqlInterpol.Parsing;

namespace SqlInterpol;

public static class SqlMetadataRegistry
{
    private static class Cache<T>
    {
        public static readonly SqlEntityMetadata Metadata = InitializeMetadata(typeof(T));
    }

    private static readonly ConcurrentDictionary<Type, SqlEntityMetadata> _runtimeCache = new();
    private static readonly ConcurrentDictionary<Type, PropertyInfo[]> _typePropertyCache = new();
    private static readonly ConcurrentDictionary<Type, Type> _entityModelTypeCache = new();

    public static SqlEntityMetadata GetMetadata<T>() => Cache<T>.Metadata;

    public static string GetEntityName(ISqlEntityBase entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        // Safely extract and cache the 'T' from ISqlEntityBase<T> (e.g. Product)
        Type modelType = _entityModelTypeCache.GetOrAdd(entity.GetType(), type =>
        {
            var interfaceType = type.GetInterfaces().FirstOrDefault(i => 
                i.IsGenericType && i.GetGenericTypeDefinition() == typeof(ISqlEntityBase<>));

            if (interfaceType != null)
            {
                // Returns the actual model type (T)
                return interfaceType.GetGenericArguments()[0];
            }

            throw new ArgumentException($"Entity type {type.Name} must implement ISqlEntityBase<T>.", nameof(entity));
        });

        // Forward the resolved model type to your existing attribute-parsing logic
        return GetMetadata(modelType).Name;
    }

    public static SqlEntityMetadata GetMetadata(Type type)
    {
        return _runtimeCache.GetOrAdd(type, InitializeMetadata);
    }

    public static PropertyInfo[] GetDtoProperties(Type type)
    {
        return _typePropertyCache.GetOrAdd(type, t => 
            t.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.GetCustomAttribute<SqlIgnoreAttribute>() == null && IsScalarType(p.PropertyType))
            .ToArray());
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

    private static SqlEntityMetadata InitializeMetadata(Type type)
    {
        var entityAttr = type.GetCustomAttribute<SqlEntityAttribute>(inherit: true);
        string name = entityAttr?.Name ?? type.Name;
        string? schema = entityAttr?.Schema; 
        SqlEntityType entityType = entityAttr?.Type ?? SqlEntityType.Table;

        var columns = new Dictionary<MemberInfo, string>();

        foreach (var prop in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            // 1. Respect explicit ignores
            if (prop.GetCustomAttribute<SqlIgnoreAttribute>() != null) continue;

            var columnAttr = prop.GetCustomAttribute<SqlColumnAttribute>();

            // 2. Strict Scalar Type Enforcement
            if (!IsScalarType(prop.PropertyType))
            {
                // Fail-fast if the user explicitly mapped an unsupported complex type
                if (columnAttr != null)
                {
                    throw new InvalidOperationException(
                        $"Property '{prop.Name}' on type '{type.Name}' is marked with [SqlColumn] but is a complex type. " +
                        "SqlInterpol only maps scalar database types. Use anonymous types for custom complex mapping.");
                }
                
                // Safely ignore navigation properties/subclasses
                continue; 
            }

            // 3. Register valid scalar columns
            columns[(MemberInfo)prop] = columnAttr?.Name ?? prop.Name;
        }

        return new SqlEntityMetadata(name, schema, entityType, columns);
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

    public static bool IsScalarType(Type type)
    {
        // Unwrap nullable types (e.g., int? -> int)
        var underlyingType = Nullable.GetUnderlyingType(type) ?? type;

        return underlyingType.IsPrimitive || 
               underlyingType.IsEnum || 
               underlyingType == typeof(string) ||
               underlyingType == typeof(decimal) ||
               underlyingType == typeof(DateTime) ||
               underlyingType == typeof(DateTimeOffset) ||
               underlyingType == typeof(TimeSpan) ||
               underlyingType == typeof(Guid) ||
               underlyingType == typeof(byte[]) ||
               underlyingType.FullName == "System.DateOnly" || // .NET 6+
               underlyingType.FullName == "System.TimeOnly";   // .NET 6+
    }
}

public record SqlEntityMetadata(
    string Name, 
    string? Schema, 
    SqlEntityType Type,
    IReadOnlyDictionary<MemberInfo, string> Columns
);