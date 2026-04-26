using System.Reflection;
using System.Runtime.CompilerServices;
using SqlInterpol.Attributes;
using SqlInterpol.Constants;
using SqlInterpol.Enums;
using SqlInterpol.Handlers;

namespace SqlInterpol.Models;

public class Sql
{
    [ThreadStatic]
    private static SqlQueryOptions? _currentOptions;

    internal static SqlQueryOptions CurrentOptions => _currentOptions ?? new SqlQueryOptions();

    internal static void SetCurrentOptions(SqlQueryOptions? options)
    {
        _currentOptions = options;
    }

    public virtual string Value { get; }

    public virtual Dictionary<string, object?> EmbeddedParameters { get; } = new();

    protected Sql(string value)
    {
        Value = value;
    }

    public override string ToString() => Value;

    public static implicit operator Sql(string? value) => new(value ?? string.Empty);

    public static SqlQuery Build([InterpolatedStringHandlerArgument] SqlQueryInterpolatedStringHandler handler)
    {
        return handler.ToQuery();
    }

    public static SqlQuery SqlQuery<T>(Func<SqlTableDefinition<T>, SqlQuery> buildQuery) where T : class, new()
    {
        return buildQuery(GetTable<T>());
    }

    public static SqlQuery Build(SqlDatabaseType database, [InterpolatedStringHandlerArgument] SqlQueryInterpolatedStringHandler handler)
    {
        var options = SqlQueryOptions.ForDatabase(database);

        return Build(options, handler);
    }

    public static SqlQuery Build(SqlQueryOptions options, [InterpolatedStringHandlerArgument] SqlQueryInterpolatedStringHandler handler)
    {
        _currentOptions = options;

        try
        {
            return handler.ToQuery(options);
        }
        finally
        {
            _currentOptions = null;
        }
    }

    public static Sql Format(string template, params object?[] args)
    {
        // Try to infer the clause from the template for proper column rendering
        string clause = InferClauseFromTemplate(template);

        return FormatWithClause(template, clause, args);
    }

    public static Sql Format(string template, string clause, params object?[] args)
    {
        return FormatWithClause(template, clause, args);
    }

    private static string InferClauseFromTemplate(string template)
    {
        // Infer the clause context from common SQL keywords in the template
        if (template.Contains(SqlKeyword.OrderBy, StringComparison.OrdinalIgnoreCase))
        {
            return SqlKeyword.OrderBy;
        }
        if (template.Contains(SqlKeyword.GroupBy, StringComparison.OrdinalIgnoreCase))
        {
            return SqlKeyword.GroupBy;
        }
        if (template.Contains(" ON ", StringComparison.OrdinalIgnoreCase) || template.Contains("ON("))
        {
            return SqlKeyword.On;
        }

        // Default to a general expression context
        return SqlKeyword.Default;
    }

    private static SqlFormat FormatWithClause(string template, string clause, object?[] args)
    {
        var result = template;
        var parameters = new Dictionary<string, object?>();
        var paramCount = 0;

        for (int i = 0; i < args.Length; i++)
        {
            var arg = args[i];
            string replacement;

            if (arg is SqlColumn sqlCol)
            {
                // Pass clause context to SqlColumn so it can decide whether to use alias or qualified name
                replacement = sqlCol.ToString(clause, Sql.CurrentOptions);
            }
            else if (arg is SqlReference sqlRef)
            {
                // Other SqlReferences render as their reference (e.g., [p].[Price])
                replacement = sqlRef.Reference;
            }
            else if (arg is Sql && !(arg is SqlReference))
            {
                // Nested Sql objects - use their value
                replacement = arg.ToString() ?? string.Empty;
            }
            else if (arg == null || (arg is string str && string.IsNullOrEmpty(str)))
            {
                // null or empty - omit
                replacement = "";
            }
            else if (arg is DBNull)
            {
                // DBNull becomes SQL NULL
                replacement = "NULL";
            }
            else
            {
                // Regular values become parameters
                string paramName = $"@p{paramCount++}";
                parameters[paramName] = arg;
                replacement = paramName;
            }

            result = result.Replace($"{{{i}}}", replacement);
        }

        // Store template and args so Build() can re-render with detected clause context
        return new SqlFormat(result, parameters, clause, template, args);
    }

    private static readonly Dictionary<Type, object> _tableCache = new();

    private record TableMetadata(string? SchemaName, string TableName, Dictionary<string, string> ColumnsByProperty);

    public static SqlTableDefinition<T> GetTable<T>() where T : class, new()
    {
        var type = typeof(T);
        var cache = _tableCache;

        // Try to get cached metadata (minimal reflection)
        if (!cache.TryGetValue(type, out var cachedMetadata))
        {
            // First call - perform reflection and cache metadata
            var tableAttr = type.GetCustomAttribute<SqlTableAttribute>();
            
            // Use attribute if present, otherwise use type name as table name and no schema
            string tableName = tableAttr?.TableName ?? type.Name;
            string? schemaName = tableAttr?.SchemaName;

            var columnsByProperty = new Dictionary<string, string>();
            var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);

            // First pass: collect explicitly marked columns
            foreach (var prop in properties)
            {
                var colAttr = prop.GetCustomAttribute<SqlColumnAttribute>();

                if (colAttr != null)
                {
                    var columnName = colAttr.ColumnName ?? prop.Name;
                    columnsByProperty[prop.Name] = columnName;
                }
            }

            // If no explicit columns found, include all properties
            if (columnsByProperty.Count == 0)
            {
                foreach (var prop in properties)
                {
                    columnsByProperty[prop.Name] = prop.Name;
                }
            }

            cachedMetadata = new TableMetadata(schemaName, tableName, columnsByProperty);
            cache[type] = cachedMetadata;
        }

        // Create fresh SqlTable and SqlColumn instances from cached metadata
        var metadata = (TableMetadata)cachedMetadata;
        var sqlTable = new SqlTable(metadata.TableName, metadata.SchemaName);
        
        var columns = new Dictionary<string, SqlColumn>();
        
        foreach (var (propName, colName) in metadata.ColumnsByProperty)
        {
            columns[propName] = new SqlColumn(sqlTable, colName);
        }

        return new SqlTableDefinition<T>(sqlTable, columns);
    }
}