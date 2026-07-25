using System;
using System.Collections.ObjectModel;

namespace SqlInterpol.Infrastructure;

/// <summary>
/// A collection that guarantees uniqueness of its items based on a provided key selector.
/// Adding a duplicate item will silently ignore the new item, preserving the original item and its order.
/// </summary>
/// <typeparam name="T">The type of elements in the collection.</typeparam>
public class UniqueCollection<T> : Collection<T>
{
    private readonly Func<T, object> _keySelector;
    private readonly System.Collections.Generic.HashSet<object> _keySet = new();

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
        if (_keySet.Add(_keySelector(item)))
            base.InsertItem(index, item);
        // Silently ignore duplicates to protect pipeline stability
    }

    /// <inheritdoc />
    protected override void SetItem(int index, T item)
    {
        if (item == null) throw new ArgumentNullException(nameof(item));
        var newKey = _keySelector(item);
        var existingKey = _keySelector(this[index]);
        if (!existingKey.Equals(newKey) && _keySet.Contains(newKey))
            return; // Silently ignore if this key already exists elsewhere
        _keySet.Remove(existingKey);
        _keySet.Add(newKey);
        base.SetItem(index, item);
    }

    /// <inheritdoc />
    protected override void RemoveItem(int index)
    {
        _keySet.Remove(_keySelector(this[index]));
        base.RemoveItem(index);
    }

    /// <inheritdoc />
    protected override void ClearItems()
    {
        _keySet.Clear();
        base.ClearItems();
    }
}