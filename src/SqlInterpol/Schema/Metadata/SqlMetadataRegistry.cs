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

    /// <summary>
    /// Returns the cached <see cref="SqlEntityMetadata"/> for the CLR type <typeparamref name="T"/>,
    /// resolving <see cref="SqlTableAttribute"/> / <see cref="SqlViewAttribute"/> and column mappings
    /// on the first call and caching the result for all subsequent calls.
    /// </summary>
    /// <typeparam name="T">The mapped entity or view type.</typeparam>
    /// <returns>The <see cref="SqlEntityMetadata"/> describing <typeparamref name="T"/>.</returns>
    public static SqlEntityMetadata GetMetadata<T>() => GetMetadata(typeof(T));

    /// <summary>
    /// Returns the cached <see cref="SqlEntityMetadata"/> for the given <paramref name="type"/>.
    /// </summary>
    /// <param name="type">The CLR type to inspect.</param>
    /// <returns>The <see cref="SqlEntityMetadata"/> describing <paramref name="type"/>.</returns>
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

    /// <summary>
    /// Returns the cached array of public, non-complex, non-ignored instance properties for
    /// <paramref name="type"/>, suitable for use in DTO-expansion scenarios.
    /// </summary>
    /// <param name="type">The DTO type whose properties to enumerate.</param>
    /// <returns>A cached <see cref="PropertyInfo"/> array for the eligible properties.</returns>
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

    /// <summary>
    /// Returns a cached, case-insensitive dictionary of compiled property getters for the
    /// given <paramref name="type"/>. Used by <see cref="SqlBuilder.Build"/> to inject
    /// template arguments in O(1) time without re-invoking reflection.
    /// </summary>
    /// <param name="type">The argument object type to build getters for.</param>
    /// <returns>
    /// A dictionary mapping property names (case-insensitive) to compiled getter delegates
    /// of the form <c>Func&lt;object, object?&gt;</c>.
    /// </returns>
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