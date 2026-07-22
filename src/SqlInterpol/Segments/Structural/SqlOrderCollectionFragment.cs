namespace SqlInterpol.Segments;

/// <summary>
/// A collection fragment for ORDER BY items, rendering each <see cref="ISqlOrderFragment"/>
/// joined by a separator.
/// </summary>
/// <param name="items">The ordered ORDER BY fragments.</param>
public class SqlOrderCollectionFragment(IEnumerable<ISqlOrderFragment> items) 
    : SqlCollectionFragmentBase<ISqlOrderFragment>(items), ISqlOrderFragment
{
}