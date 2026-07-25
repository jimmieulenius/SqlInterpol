using System.Collections.Concurrent;
using System.Reflection;

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

    public static SqlEntityMetadata GetMetadata<T>() => GetMetadata(typeof(T));

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
                name = tableAttr.Name ?? t.Name;
                schema = tableAttr.Schema;
                entityType = SqlEntityType.Table;
            }
            else
            {
                var viewAttr = t.GetCustomAttribute<SqlViewAttribute>();
                if (viewAttr != null)
                {
                    name = viewAttr.Name ?? t.Name;
                    schema = viewAttr.Schema;
                    entityType = SqlEntityType.View;
                }
            }

            var columns = new Dictionary<PropertyInfo, string>();

            foreach (var prop in t.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                if (prop.GetCustomAttribute<SqlIgnoreAttribute>() != null) continue;

                // FIX: Ignore complex object types by default to prevent nested object leak into SQL mapping
                var propType = prop.PropertyType;
                if (propType.IsClass && propType != typeof(string) && propType != typeof(byte[])) continue;

                var colAttr = prop.GetCustomAttribute<SqlColumnAttribute>();
                columns[prop] = colAttr?.Name ?? prop.Name;
            }

            return new SqlEntityMetadata(name, schema, entityType, columns);
        });
    }

    public static PropertyInfo[] GetDtoProperties(Type type)
    {
        return _dtoPropertyCache.GetOrAdd(type, t => t.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => 
            {
                if (p.GetCustomAttribute<SqlIgnoreAttribute>() != null) return false;
                
                // FIX: Ignore complex object types by default to prevent nested object leak into SQL mapping
                var propType = p.PropertyType;
                if (propType.IsClass && propType != typeof(string) && propType != typeof(byte[])) return false;

                return true;
            })
            .ToArray());
    }

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