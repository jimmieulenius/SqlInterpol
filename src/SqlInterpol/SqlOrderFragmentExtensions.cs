using System.Collections.Generic;

namespace SqlInterpol;

public static class SqlOrderFragmentExtensions
{
    extension(ISqlOrderFragment current)
    {
        public ISqlOrderFragment ThenBy(
            ISqlReference reference,
            SqlOrderDirection direction = SqlOrderDirection.Asc)
        {
            var next = new SqlOrderFragment(reference, direction);
            return Combine(current, next);
        }

        public ISqlOrderFragment ThenBy<T>(
            ISqlEntityBase<T> entity,
            string propertyName,
            SqlOrderDirection direction = SqlOrderDirection.Asc)
        {
            var next = entity.OrderBy(propertyName, direction); 
            return Combine(current, next);
        }
    }

    private static ISqlOrderFragment Combine(ISqlOrderFragment current, ISqlOrderFragment next)
    {
        var list = new List<ISqlOrderFragment>();

        if (current is SqlOrderCollectionFragment collection)
        {
            list.AddRange(collection.Items);
        }
        else
        {
            list.Add(current);
        }

        list.Add(next);

        return new SqlOrderCollectionFragment(list);
    }
}