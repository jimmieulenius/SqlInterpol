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

    public static SqlEntityMetadata GetMetadata<T>() => Cache<T>.Metadata;

    private static SqlEntityMetadata InitializeMetadata(Type type)
    {
        // 1. Look for the BASE attribute. 
        // This captures [SqlTable] or [SqlView] correctly.
        var entityAttr = type.GetCustomAttribute<SqlEntityAttribute>(inherit: true);
        
        // 2. Extract properties from the attribute
        string name = entityAttr?.Name ?? type.Name;
        string? schema = entityAttr?.Schema; // This should now be populated
        SqlEntityType entityType = entityAttr?.Type ?? SqlEntityType.Table;

        // 3. Map columns
        var columns = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .ToDictionary(
                p => (MemberInfo)p,
                p => p.GetCustomAttribute<SqlColumnAttribute>()?.Name ?? p.Name
            );

        return new SqlEntityMetadata(name, schema, entityType, columns);
    }

    // Resolves p[x => x.Name] to "[Name]"
    // Inside SqlMetadataRegistry.cs
    public static string GetColumnName<T>(Expression<Func<T, object>> propertySelector)
    {
        // Use your helper to get the MemberInfo
        var member = SqlExpressionHelper.GetMember(propertySelector);
        var meta = GetMetadata<T>();

        // Support for [SqlColumn("custom_name")]
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

        // Handle boxing for value types: Convert(x.Id, Object)
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