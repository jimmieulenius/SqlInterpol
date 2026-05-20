using System.Linq.Expressions;
using SqlInterpol.Parsing;

namespace SqlInterpol;

public static class SqlEntityExtensions
{
    public static ISqlEntityBase<T> As<T>(this ISqlEntityBase<T> entity, string alias)
    {
        if (entity.Reference != null)
        {
            entity.Reference.Alias = alias;
            entity.Reference.IsAliasQuoted = true;
        }
        
        return entity;
    }

    public static ISqlOrderFragment OrderBy<T>(
        this ISqlEntityBase<T> entity,
        string propertyName, 
        SqlOrderDirection? direction = null)
    {
        var meta = SqlMetadataRegistry.GetMetadata<T>();
        
        var columnMap = meta.Columns.FirstOrDefault(c => 
            c.Key.Name.Equals(propertyName, StringComparison.OrdinalIgnoreCase));

        if (columnMap.Key == null)
        {
            throw new ArgumentException($"Property '{propertyName}' not found on '{typeof(T).Name}'.");
        }

        return new SqlOrderFragment(entity, columnMap.Value, direction);
    }

    public static ISqlOrderFragment OrderBy<T>(
        this ISqlEntityBase<T> entity,
        Expression<Func<T, object?>> expression, 
        SqlOrderDirection? direction = null)
    {
        var memberInfo = SqlExpressionHelper.GetMember(expression);
        var meta = SqlMetadataRegistry.GetMetadata<T>();

        if (!meta.Columns.TryGetValue(memberInfo, out string? physicalName))
        {
            throw new ArgumentException($"Property '{memberInfo.Name}' not mapped.");
        }

        return new SqlOrderFragment(entity, physicalName, direction);
    }
}