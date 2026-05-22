namespace SqlInterpol;

/// <summary>
/// A collection fragment for heterogeneous <see cref="ISqlFragment"/> items,
/// rendering them joined by a separator.
/// </summary>
/// <param name="items">The fragments to include in the collection.</param>
/// <param name="separator">Optional separator override; defaults to <see cref="SqlInterpolOptions.CollectionSeparator"/>.</param>
public class SqlCollectionFragment(IEnumerable<ISqlFragment> items, string? separator = null) 
    : SqlCollectionFragmentBase<ISqlFragment>(items, separator)
{
}