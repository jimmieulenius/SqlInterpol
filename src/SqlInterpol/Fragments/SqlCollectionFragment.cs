namespace SqlInterpol;

public class SqlCollectionFragment(IEnumerable<ISqlFragment> items, string? separator = null) 
    : SqlCollectionFragmentBase<ISqlFragment>(items, separator)
{
}