namespace SqlInterpol.Segments;

/// <summary>
/// A collection fragment for heterogeneous <see cref="ISqlFragment"/> items.
/// </summary>
public class SqlCollectionFragment(IEnumerable<ISqlFragment> items, string? separator = null) 
    : SqlCollectionFragmentBase<ISqlFragment>(items, separator);