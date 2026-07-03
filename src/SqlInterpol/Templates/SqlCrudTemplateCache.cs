using System.Collections.Concurrent;

namespace SqlInterpol;

/// <summary>
/// Globally caches auto-generated, dialect-specific CRUD templates.
/// </summary>
internal static class SqlCrudTemplateCache
{
    private static readonly ConcurrentDictionary<(Type, Type, SqlDialectKind), ISqlTemplate> _insertCache = new();

    public static ISqlTemplate GetInsertTemplate<TEntity, TDto>(ISqlDialect dialect)
    {
        return _insertCache.GetOrAdd((typeof(TEntity), typeof(TDto), dialect.Kind), _ => 
        {
            var meta = SqlMetadataRegistry.GetMetadata<TEntity>();
            var props = SqlMetadataRegistry.GetDtoProperties(typeof(TDto));
            
            var tableName = dialect.QuoteIdentifier(meta.Name);
            if (!string.IsNullOrEmpty(meta.Schema))
                tableName = $"{dialect.QuoteIdentifier(meta.Schema)}.{tableName}";

            var cols = new List<string>();
            var extractors = new List<Func<object, object?>>();
            var getters = SqlMetadataRegistry.GetArgumentGetters(typeof(TDto));

            foreach (var prop in props)
            {
                var matchingKey = meta.Columns.Keys.FirstOrDefault(k => k.Name.Equals(prop.Name, StringComparison.OrdinalIgnoreCase));
                if (matchingKey != null)
                {
                    var colName = meta.Columns[matchingKey];
                    cols.Add(dialect.QuoteIdentifier(colName));
                    extractors.Add(getters[prop.Name]);
                }
            }

            var holes = string.Join(", ", Enumerable.Range(0, cols.Count).Select(i => $"{{{i}}}"));
            string rowFormat = $"({holes})";

            return new SqlBulkTemplate<TDto>(tableName, cols, rowFormat, item => 
            {
                var vals = new object?[extractors.Count];
                for (int i = 0; i < extractors.Count; i++) vals[i] = extractors[i](item!);
                return vals;
            });
        });
    }
}