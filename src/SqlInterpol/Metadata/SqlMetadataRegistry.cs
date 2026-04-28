using System.Collections.Concurrent;
using System.Reflection;

namespace SqlInterpol.Metadata;

internal static class SqlMetadataRegistry
{
    private static readonly ConcurrentDictionary<Type, TableMetadata> _cache = new();

    public static TableMetadata GetMetadata<T>()
    {
        return _cache.GetOrAdd(typeof(T), type =>
        {
            var tableAttr = type.GetCustomAttribute<SqlTableAttribute>();
            string tableName = tableAttr?.Name ?? (type.Name + "s");
            string? schema = tableAttr?.Schema;

            // Ensure we filter for non-null attribute names and cast to non-nullable string
            var columnOverrides = type.GetProperties()
                .Select(p => new { p.Name, Attr = p.GetCustomAttribute<SqlColumnAttribute>() })
                .Where(x => x.Attr != null && !string.IsNullOrEmpty(x.Attr.Name))
                .ToDictionary(
                    x => x.Name, 
                    x => x.Attr!.Name! // The ! tells the compiler we know these aren't null
                );

            return new TableMetadata(tableName, schema, columnOverrides);
        });
    }
}

internal record TableMetadata(
    string TableName, 
    string? Schema, 
    Dictionary<string, string> ColumnOverrides
);