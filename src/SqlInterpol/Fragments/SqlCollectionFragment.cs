namespace SqlInterpol;

/// <summary>
/// A collection fragment for heterogeneous <see cref="ISqlFragment"/> items,
/// rendering them joined by a separator.
/// </summary>
/// <param name="items">The fragments to include in the collection.</param>
/// <param name="separator">Optional separator override; defaults to <see cref="SqlInterpolOptions.CollectionSeparator"/>.</param>
public class SqlCollectionFragment(IEnumerable<ISqlFragment> items, string? separator = null) 
    : SqlCollectionFragmentBase<ISqlFragment>(items, separator), ISqlSwappableFragment
{
    /// <inheritdoc />
    public override ISqlFragment Swap(
        Dictionary<ISqlReference, ISqlEntityBase> entityMap, 
        IReadOnlyDictionary<string, Func<object, object?>>? argumentGetters, 
        object? arguments)
    {
        var mappedItems = new List<ISqlFragment>(Items.Count);
        
        foreach (var item in Items)
        {
            if (item is ISqlSwappableFragment swappable)
            {
                mappedItems.Add(swappable.Swap(entityMap, argumentGetters, arguments));
            }
            else if (item is SqlColumnReferenceBase colRef && entityMap.TryGetValue(colRef.SourceReference, out var realEntity))
            {
                mappedItems.Add(colRef is SqlRawColumnReference 
                    ? (SqlColumnReferenceBase)new SqlRawColumnReference(realEntity.Reference, colRef.ColumnName)
                    : new SqlColumnReference(realEntity.Reference, colRef.ColumnName, colRef.PropertyName));
            }
            else
            {
                mappedItems.Add(item);
            }
        }

        return new SqlCollectionFragment(mappedItems, Separator);
    }
}