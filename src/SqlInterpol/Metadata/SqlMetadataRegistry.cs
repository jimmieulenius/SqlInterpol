using System.Linq.Expressions;
using System.Reflection;
using SqlInterpol.Parsing;

namespace SqlInterpol.Metadata;

public static class SqlMetadataRegistry
{
    private static class Cache<T>
    {
        public static readonly EntityMetadata Metadata = InitializeMetadata(typeof(T));
    }

    public static EntityMetadata GetMetadata<T>() => Cache<T>.Metadata;

    private static EntityMetadata InitializeMetadata(Type type)
    {
        var tableAttr = type.GetCustomAttribute<SqlTableAttribute>();
        
        string name = tableAttr?.Name ?? type.Name;
        string? schema = tableAttr?.Schema;

        // Discover all properties with [Column] attribute or use naming convention
        var columns = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .ToDictionary(
                p => (MemberInfo)p,
                p => p.GetCustomAttribute<SqlColumnAttribute>()?.Name ?? p.Name
            );

        return new EntityMetadata(name, schema, columns);
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

public record EntityMetadata(
    string Name, 
    string? Schema, 
    IReadOnlyDictionary<MemberInfo, string> Columns
);