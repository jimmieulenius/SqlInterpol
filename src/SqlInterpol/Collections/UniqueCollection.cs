using System.Collections.ObjectModel;

namespace SqlInterpol;

/// <summary>
/// A collection that guarantees uniqueness of its items based on a provided key selector.
/// Adding a duplicate item will silently ignore the new item, preserving the original item and its order.
/// </summary>
/// <typeparam name="T">The type of elements in the collection.</typeparam>
public class UniqueCollection<T> : Collection<T>
{
    private readonly Func<T, object> _keySelector;

    /// <summary>
    /// Initializes a new instance of the <see cref="UniqueCollection{T}"/> class.
    /// </summary>
    /// <param name="keySelector">A function to extract the uniqueness key from an item.</param>
    public UniqueCollection(Func<T, object> keySelector)
    {
        _keySelector = keySelector ?? throw new ArgumentNullException(nameof(keySelector));
    }

    /// <inheritdoc />
    protected override void InsertItem(int index, T item)
    {
        if (item == null) throw new ArgumentNullException(nameof(item));
        
        var key = _keySelector(item);
        if (this.Any(x => _keySelector(x).Equals(key)))
        {
            return; // Silently ignore duplicates to protect pipeline stability
        }

        base.InsertItem(index, item);
    }

    /// <inheritdoc />
    protected override void SetItem(int index, T item)
    {
        if (item == null) throw new ArgumentNullException(nameof(item));
        
        var key = _keySelector(item);
        var existingIdx = IndexOfKey(key);
        
        if (existingIdx >= 0 && existingIdx != index)
        {
            return; // Silently ignore if this key already exists elsewhere
        }

        base.SetItem(index, item);
    }

    private int IndexOfKey(object key)
    {
        for (int i = 0; i < Count; i++)
        {
            if (_keySelector(this[i]).Equals(key)) return i;
        }
        return -1;
    }
}