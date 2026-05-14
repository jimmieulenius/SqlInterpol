using System.Linq.Expressions;
using SqlInterpol.Metadata;
using SqlInterpol.Parsing;

namespace SqlInterpol;

public static class SqlEntityExtensions
{
    extension<T>(ISqlEntityBase<T> entity)
    {
        public ISqlEntityBase<T> As(string alias)
        {
            entity.Reference?.Alias = alias;
            
            return entity;
        }

        public ISqlOrderFragment OrderBy(
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

        public ISqlOrderFragment OrderBy(
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
}