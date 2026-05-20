namespace SqlInterpol;

public class SqlOrderCollectionFragment(IEnumerable<ISqlOrderFragment> items) 
    : SqlCollectionFragmentBase<ISqlOrderFragment>(items), ISqlOrderFragment
{
}