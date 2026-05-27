namespace SqlInterpol;

/// <summary>
/// A collection fragment for ORDER BY items, rendering each <see cref="ISqlOrderFragment"/>
/// joined by a separator.
/// </summary>
/// <param name="items">The ordered ORDER BY fragments.</param>
public class SqlOrderCollectionFragment(IEnumerable<ISqlOrderFragment> items) 
    : SqlCollectionFragmentBase<ISqlOrderFragment>(items), ISqlOrderFragment, ISqlSwappableFragment
{
    /// <inheritdoc />
    public override ISqlFragment Swap(
        Dictionary<ISqlReference, ISqlEntityBase> entityMap, 
        IReadOnlyDictionary<string, Func<object, object?>>? argumentGetters, 
        object? arguments)
    {
        var mappedItems = new List<ISqlOrderFragment>(Items.Count);
        
        foreach (var item in Items)
        {
            if (item is ISqlSwappableFragment swappable)
            {
                // Swap the inner order fragment (SqlOrderFragment is swappable)
                mappedItems.Add((ISqlOrderFragment)swappable.Swap(entityMap, argumentGetters, arguments));
            }
            else
            {
                mappedItems.Add(item);
            }
        }

        return new SqlOrderCollectionFragment(mappedItems);
    }
}