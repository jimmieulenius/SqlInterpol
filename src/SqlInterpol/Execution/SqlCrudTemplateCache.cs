using System.Collections.Concurrent;
using SqlInterpol.Configuration;
using SqlInterpol.Schema;

namespace SqlInterpol.Execution;

/// <summary>
/// Globally caches auto-generated, dialect-specific CRUD templates.
/// </summary>
internal static class SqlCrudTemplateCache
{
    private static readonly ConcurrentDictionary<(Type, Type, SqlDialectKind), ISqlTemplate> _insertCache = new();
    private static readonly ConcurrentDictionary<(Type, Type, SqlDialectKind, string), ISqlTemplate> _updateCache = new();
    private static readonly ConcurrentDictionary<(Type, Type, SqlDialectKind, string), ISqlTemplate> _deleteCache = new();

    private static Type UnwrapType(Type type)
    {
        if (type == typeof(string)) return type;
        if (type.IsArray) return type.GetElementType()!;
        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IEnumerable<>)) return type.GetGenericArguments()[0];
        
        var ienum = type.GetInterfaces().FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEnumerable<>));
        if (ienum != null) return ienum.GetGenericArguments()[0];
        
        return type;
    }

    public static ISqlTemplate GetInsertTemplate<TEntity, TDto>(ISqlDialect dialect)
    {
        Type dtoType = UnwrapType(typeof(TDto));
        return _insertCache.GetOrAdd((typeof(TEntity), dtoType, dialect.Kind), _ => 
        {
            var meta = SqlMetadataRegistry.GetMetadata<TEntity>();
            var props = SqlMetadataRegistry.GetDtoProperties(dtoType);
            
            var tableName = dialect.QuoteIdentifier(meta.Name);
            if (!string.IsNullOrEmpty(meta.Schema))
                tableName = $"{dialect.QuoteIdentifier(meta.Schema)}.{tableName}";
                
            var cols = new List<string>();
            var extractors = new List<Func<object, object?>>();
            var getters = SqlMetadataRegistry.GetArgumentGetters(dtoType);
            
            foreach (var prop in props)
            {
                var matchingKey = meta.Columns.Keys.FirstOrDefault(k => k.Name.Equals(prop.Name, StringComparison.OrdinalIgnoreCase));
                if (matchingKey != null)
                {
                    cols.Add(dialect.QuoteIdentifier(meta.Columns[matchingKey]));
                    extractors.Add(getters[prop.Name]);
                }
            }
            
            var holes = string.Join(", ", Enumerable.Range(0, cols.Count).Select(i => $"{{{i}}}"));
            string rowFormat = $"({holes})";
            
            return new SqlBulkTemplate(tableName, cols, rowFormat, item => 
            {
                var vals = new object?[extractors.Count];
                for (int i = 0; i < extractors.Count; i++) vals[i] = extractors[i](item!);
                return vals;
            });
        });
    }

    public static ISqlTemplate GetUpdateTemplate<TEntity, TDto>(ISqlDialect dialect, string[] keyProperties)
    {
        Type dtoType = UnwrapType(typeof(TDto));
        var cacheKey = (typeof(TEntity), dtoType, dialect.Kind, string.Join("|", keyProperties));
        
        return _updateCache.GetOrAdd(cacheKey, _ => 
        {
            var meta = SqlMetadataRegistry.GetMetadata<TEntity>();
            var props = SqlMetadataRegistry.GetDtoProperties(dtoType);
            
            var tableName = dialect.QuoteIdentifier(meta.Name);
            if (!string.IsNullOrEmpty(meta.Schema))
                tableName = $"{dialect.QuoteIdentifier(meta.Schema)}.{tableName}";
                
            var setCols = new List<string>();
            var whereCols = new List<string>();
            var extractors = new List<Func<object, object?>>();
            var getters = SqlMetadataRegistry.GetArgumentGetters(dtoType);
            
            int paramIndex = 0;
            var setHoles = new List<string>();
            var whereHoles = new List<string>();
            
            foreach (var prop in props.Where(p => !keyProperties.Contains(p.Name, StringComparer.OrdinalIgnoreCase)))
            {
                var matchingKey = meta.Columns.Keys.FirstOrDefault(k => k.Name.Equals(prop.Name, StringComparison.OrdinalIgnoreCase));
                if (matchingKey != null)
                {
                    setCols.Add(dialect.QuoteIdentifier(meta.Columns[matchingKey]));
                    extractors.Add(getters[prop.Name]);
                    setHoles.Add($"{{{paramIndex++}}}");
                }
            }
            
            foreach (var keyName in keyProperties)
            {
                var prop = props.FirstOrDefault(p => p.Name.Equals(keyName, StringComparison.OrdinalIgnoreCase));
                if (prop != null)
                {
                    var matchingKey = meta.Columns.Keys.FirstOrDefault(k => k.Name.Equals(prop.Name, StringComparison.OrdinalIgnoreCase));
                    if (matchingKey != null)
                    {
                        whereCols.Add(dialect.QuoteIdentifier(meta.Columns[matchingKey]));
                        extractors.Add(getters[prop.Name]);
                        whereHoles.Add($"{{{paramIndex++}}}");
                    }
                }
            }
            
            if (setCols.Count == 0) throw new InvalidOperationException($"No updatable columns found for {dtoType.Name}.");
            if (whereCols.Count == 0) throw new InvalidOperationException($"No key columns defined for UPDATE on {typeof(TEntity).Name}.");
            
            var setClause = string.Join(", ", setCols.Select((c, i) => $"{c} = {setHoles[i]}"));
            var whereClause = string.Join(" AND ", whereCols.Select((c, i) => $"{c} = {whereHoles[i]}"));
            
            string statementFormat = $"UPDATE {tableName} SET {setClause} WHERE {whereClause}";
            
            return new SqlBatchTemplate(statementFormat, item => 
            {
                var vals = new object?[extractors.Count];
                for (int i = 0; i < extractors.Count; i++) vals[i] = extractors[i](item!);
                return vals;
            });
        });
    }

    public static ISqlTemplate GetDeleteTemplate<TEntity, TDto>(ISqlDialect dialect, string[] keyProperties)
    {
        Type dtoType = UnwrapType(typeof(TDto));
        var cacheKey = (typeof(TEntity), dtoType, dialect.Kind, string.Join("|", keyProperties));
        
        return _deleteCache.GetOrAdd(cacheKey, _ => 
        {
            var meta = SqlMetadataRegistry.GetMetadata<TEntity>();
            var props = SqlMetadataRegistry.GetDtoProperties(dtoType);
            
            var tableName = dialect.QuoteIdentifier(meta.Name);
            if (!string.IsNullOrEmpty(meta.Schema))
                tableName = $"{dialect.QuoteIdentifier(meta.Schema)}.{tableName}";
                
            var whereCols = new List<string>();
            var extractors = new List<Func<object, object?>>();
            var getters = SqlMetadataRegistry.GetArgumentGetters(dtoType);
            
            int paramIndex = 0;
            var whereHoles = new List<string>();
            
            foreach (var keyName in keyProperties)
            {
                var prop = props.FirstOrDefault(p => p.Name.Equals(keyName, StringComparison.OrdinalIgnoreCase));
                if (prop != null)
                {
                    var matchingKey = meta.Columns.Keys.FirstOrDefault(k => k.Name.Equals(prop.Name, StringComparison.OrdinalIgnoreCase));
                    if (matchingKey != null)
                    {
                        whereCols.Add(dialect.QuoteIdentifier(meta.Columns[matchingKey]));
                        extractors.Add(getters[prop.Name]);
                        whereHoles.Add($"{{{paramIndex++}}}");
                    }
                }
            }
            
            if (whereCols.Count == 0) throw new InvalidOperationException($"No key columns defined for DELETE on {typeof(TEntity).Name}.");
            
            var whereClause = string.Join(" AND ", whereCols.Select((c, i) => $"{c} = {whereHoles[i]}"));
            string statementFormat = $"DELETE FROM {tableName} WHERE {whereClause}";
            
            return new SqlBatchTemplate(statementFormat, item => 
            {
                var vals = new object?[extractors.Count];
                for (int i = 0; i < extractors.Count; i++) vals[i] = extractors[i](item!);
                return vals;
            });
        });
    }
}