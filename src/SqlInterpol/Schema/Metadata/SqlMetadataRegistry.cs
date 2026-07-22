using System.Collections.Concurrent;
using System.Reflection;
using SqlInterpol.Configuration;

namespace SqlInterpol.Schema;

/// <summary>
/// A thread-safe static registry that caches reflection metadata, property maps, and 
/// compiled argument getters for mapped entities and dynamic templates.
/// </summary>
public static class SqlMetadataRegistry
{
    private static readonly ConcurrentDictionary<Type, SqlEntityMetadata> _metadataCache = new();
    private static readonly ConcurrentDictionary<Type, PropertyInfo[]> _dtoPropertyCache = new();
    private static readonly ConcurrentDictionary<Type, IReadOnlyDictionary<string, Func<object, object?>>> _getterCache = new();

    /// <summary>
    /// Retrieves or builds the metadata for a given entity type.
    /// </summary>
    /// <typeparam name="T">The entity model type.</typeparam>
    /// <returns>The cached entity metadata.</returns>
    public static SqlEntityMetadata GetMetadata<T>() => GetMetadata(typeof(T));

    /// <summary>
    /// Retrieves or builds the metadata for a given entity type.
    /// </summary>
    /// <param name="type">The entity model type.</param>
    /// <returns>The cached entity metadata.</returns>
    public static SqlEntityMetadata GetMetadata(Type type)
    {
        return _metadataCache.GetOrAdd(type, t =>
        {
            string name = t.Name;
            string? schema = null;
            SqlEntityType entityType = SqlEntityType.Table;

            var tableAttr = t.GetCustomAttribute<SqlTableAttribute>();
            if (tableAttr != null)
            {
                name = tableAttr.Name;
                schema = tableAttr.Schema;
                entityType = SqlEntityType.Table;
            }
            else
            {
                var viewAttr = t.GetCustomAttribute<SqlViewAttribute>();
                if (viewAttr != null)
                {
                    name = viewAttr.Name;
                    schema = viewAttr.Schema;
                    entityType = SqlEntityType.View;
                }
            }

            var columns = new Dictionary<PropertyInfo, string>();
            foreach (var prop in t.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                if (prop.GetCustomAttribute<SqlIgnoreAttribute>() != null) continue;

                var colAttr = prop.GetCustomAttribute<SqlColumnAttribute>();
                columns[prop] = colAttr?.Name ?? prop.Name;
            }

            return new SqlEntityMetadata(name, schema, entityType, columns);
        });
    }

    /// <summary>
    /// Retrieves the public instance properties of a DTO or anonymous type, ignoring those marked with <see cref="SqlIgnoreAttribute"/>.
    /// </summary>
    /// <param name="type">The DTO type.</param>
    /// <returns>An array of property information.</returns>
    public static PropertyInfo[] GetDtoProperties(Type type)
    {
        return _dtoPropertyCache.GetOrAdd(type, t => t.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.GetCustomAttribute<SqlIgnoreAttribute>() == null)
            .ToArray());
    }

    /// <summary>
    /// Generates and caches highly optimized getter delegates for a type's properties.
    /// </summary>
    /// <param name="type">The target type to extract properties from.</param>
    /// <returns>A dictionary mapping property names to their compiled getter delegates.</returns>
    public static IReadOnlyDictionary<string, Func<object, object?>> GetArgumentGetters(Type type)
    {
        return _getterCache.GetOrAdd(type, t =>
        {
            var dict = new Dictionary<string, Func<object, object?>>(StringComparer.OrdinalIgnoreCase);
            var props = t.GetProperties(BindingFlags.Public | BindingFlags.Instance);
            foreach (var prop in props)
            {
                var localProp = prop;
                dict[prop.Name] = obj => localProp.GetValue(obj);
            }
            return dict;
        });
    }
}