namespace SqlInterpol;

/// <summary>
/// Base class for SQL fragment collections, rendering items joined by a separator
/// with support for horizontal and <see cref="SqlCollectionLayout.Vertical"/> layouts.
/// </summary>
/// <typeparam name="T">The fragment element type.</typeparam>
/// <param name="items">The fragments to include in the collection.</param>
/// <param name="separator">
/// The separator string between items. When <see langword="null"/>, falls back to
/// <see cref="SqlInterpolOptions.CollectionSeparator"/>.
/// </param>
public class SqlCollectionFragmentBase<T>(IEnumerable<T> items, string? separator = null) 
    : ISqlFragment, ISqlSwappableFragment where T : ISqlFragment
{
    /// <summary>Gets the optional explicit separator string assigned to this collection.</summary>
    protected string? Separator { get; } = separator;

    /// <summary>Gets the ordered list of fragment items in this collection.</summary>
    public IReadOnlyList<T> Items { get; } = [.. items];

    /// <inheritdoc />
    public string ToSql(ISqlContext context, SqlRenderMode mode = SqlRenderMode.Default)
    {
        string currentSeparator = Separator ?? context.Options.CollectionSeparator;
        var list = Items.Select(i => i.ToSql(context, mode)).ToList();

        if (list.Count == 0)
        {
            return string.Empty;
        }

        if (context.Options.CollectionLayout == SqlCollectionLayout.Vertical)
        {
            var indent = new string(' ', context.Options.IndentSize);
            
            return $"{Environment.NewLine}{indent}{string.Join($"{currentSeparator.TrimEnd()}{Environment.NewLine}{indent}", list)}";
        }

        return string.Join(currentSeparator, list);
    }

    /// <inheritdoc />
    public virtual ISqlFragment Swap(
        Dictionary<ISqlReference, ISqlEntityBase> entityMap, 
        IReadOnlyDictionary<string, Func<object, object?>>? argumentGetters, 
        object? arguments)
    {
        var mappedItems = new List<T>(Items.Count);
        
        foreach (var item in Items)
        {
            if (item is ISqlSwappableFragment swappable)
            {
                mappedItems.Add((T)swappable.Swap(entityMap, argumentGetters, arguments));
            }
            else if (item is SqlColumnReferenceBase colRef && entityMap.TryGetValue(colRef.SourceReference, out var realEntity))
            {
                mappedItems.Add((T)(ISqlFragment)(colRef is SqlRawColumnReference 
                    ? (SqlColumnReferenceBase)new SqlRawColumnReference(realEntity.Reference, colRef.ColumnName)
                    : new SqlColumnReference(realEntity.Reference, colRef.ColumnName, colRef.PropertyName)));
            }
            else
            {
                mappedItems.Add(item);
            }
        }

        return new SqlCollectionFragmentBase<T>(mappedItems, Separator);
    }
}