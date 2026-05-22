using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reflection;
using SqlInterpol.Parsing;

namespace SqlInterpol;

/// <summary>
/// Thread-safe registry for SQL entity metadata. Caches column mappings, entity names,
/// and schema information derived from attributes and CLR reflection.
/// </summary>
/// <remarks>
/// Metadata is computed once per type using generic static caching (<c>Cache&lt;T&gt;</c>) for
/// zero-allocation lookup on the hot path, and a <see cref="ConcurrentDictionary{TKey,TValue}"/>
/// for dynamic runtime type lookups.
/// </remarks>
public static class SqlMetadataRegistry
{
    private static class Cache<T>
    {
        public static readonly SqlEntityMetadata Metadata = InitializeMetadata(typeof(T));
    }

    private static readonly ConcurrentDictionary<Type, SqlEntityMetadata> _runtimeCache = new();
    private static readonly ConcurrentDictionary<Type, PropertyInfo[]> _typePropertyCache = new();
    private static readonly ConcurrentDictionary<Type, Type> _entityModelTypeCache = new();

    /// <summary>Gets the cached <see cref="SqlEntityMetadata"/> for <typeparamref name="T"/>.</summary>
    /// <typeparam name="T">The CLR type to retrieve metadata for.</typeparam>
    /// <returns>The <see cref="SqlEntityMetadata"/> for <typeparamref name="T"/>.</returns>
    public static SqlEntityMetadata GetMetadata<T>() => Cache<T>.Metadata;

    /// <summary>
    /// Resolves the physical entity name from an <see cref="ISqlEntityBase"/> instance by
    /// reflecting its generic <c>T</c> argument and consulting the metadata registry.
    /// </summary>
    /// <param name="entity">The entity whose name to resolve.</param>
    /// <returns>The physical table or view name for the entity's mapped type.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="entity"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">Thrown when the entity type does not implement <see cref="ISqlEntityBase{T}"/>.</exception>
    public static string GetEntityName(ISqlEntityBase entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        Type modelType = _entityModelTypeCache.GetOrAdd(entity.GetType(), type =>
        {
            var interfaceType = type.GetInterfaces().FirstOrDefault(i => 
                i.IsGenericType && i.GetGenericTypeDefinition() == typeof(ISqlEntityBase<>));

            if (interfaceType != null)
            {
                return interfaceType.GetGenericArguments()[0];
            }

            throw new ArgumentException($"Entity type {type.Name} must implement ISqlEntityBase<T>.", nameof(entity));
        });

        return GetMetadata(modelType).Name;
    }

    /// <summary>Gets or creates the <see cref="SqlEntityMetadata"/> for the specified type.</summary>
    /// <param name="type">The CLR type to retrieve metadata for.</param>
    /// <returns>The <see cref="SqlEntityMetadata"/> for <paramref name="type"/>.</returns>
    public static SqlEntityMetadata GetMetadata(Type type)
    {
        return _runtimeCache.GetOrAdd(type, InitializeMetadata);
    }

    /// <summary>
    /// Returns the scalar, non-ignored public instance properties for the specified type.
    /// Used when mapping query results to DTO types.
    /// </summary>
    /// <param name="type">The CLR type to inspect.</param>
    /// <returns>
    /// The array of properties that are scalar types and not marked with <see cref="SqlIgnoreAttribute"/>.
    /// </returns>
    public static PropertyInfo[] GetDtoProperties(Type type)
    {
        return _typePropertyCache.GetOrAdd(type, t => 
            t.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.GetCustomAttribute<SqlIgnoreAttribute>() == null && IsScalarType(p.PropertyType))
            .ToArray());
    }

    /// <summary>
    /// Gets the physical column name for the property selected by the expression.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="propertySelector">A lambda expression selecting a property of <typeparamref name="T"/>.</param>
    /// <returns>
    /// The physical column name — either the value from <see cref="SqlColumnAttribute"/> or the property name.
    /// </returns>
    /// <exception cref="ArgumentException">Thrown when the selected property is not found in the cached metadata.</exception>
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

    /// <summary>Gets the CLR property name from the property selector expression.</summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="propertySelector">A lambda expression selecting a property of <typeparamref name="T"/>.</param>
    /// <returns>The CLR property name (not the column name override).</returns>
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
            if (prop.GetCustomAttribute<SqlIgnoreAttribute>() != null) continue;

            var columnAttr = prop.GetCustomAttribute<SqlColumnAttribute>();

            if (!IsScalarType(prop.PropertyType))
            {
                if (columnAttr != null)
                {
                    throw new InvalidOperationException(
                        $"Property '{prop.Name}' on type '{type.Name}' is marked with [SqlColumn] but is a complex type. " +
                        "SqlInterpol only maps scalar database types. Use anonymous types for custom complex mapping.");
                }
                
                continue; 
            }

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

    /// <summary>
    /// Determines whether a CLR type is a supported scalar database type.
    /// Nullable types are unwrapped before checking.
    /// </summary>
    /// <param name="type">The type to check.</param>
    /// <returns>
    /// <see langword="true"/> for primitives, enums, <see cref="string"/>, <see cref="decimal"/>,
    /// <see cref="DateTime"/>, <see cref="DateTimeOffset"/>, <see cref="TimeSpan"/>, <see cref="Guid"/>,
    /// <c>byte[]</c>, <c>DateOnly</c>, and <c>TimeOnly</c>; otherwise <see langword="false"/>.
    /// </returns>
    public static bool IsScalarType(Type type)
    {
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
               underlyingType.FullName == "System.DateOnly" ||
               underlyingType.FullName == "System.TimeOnly";
    }
}

/// <summary>
/// Holds the resolved metadata for a SQL entity type: physical name, schema, kind, and column mappings.
/// </summary>
/// <param name="Name">The physical table or view name.</param>
/// <param name="Schema">The schema, or <see langword="null"/> for the default schema.</param>
/// <param name="Type">Whether this entity is a <see cref="SqlEntityType.Table"/>, <see cref="SqlEntityType.View"/>, or subquery.</param>
/// <param name="Columns">
/// A map from each <see cref="MemberInfo"/> to its physical column name,
/// respecting any <see cref="SqlColumnAttribute"/> override.
/// </param>
public record SqlEntityMetadata(
    string Name, 
    string? Schema, 
    SqlEntityType Type,
    IReadOnlyDictionary<MemberInfo, string> Columns
);