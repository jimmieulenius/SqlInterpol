
namespace SqlInterpol;

public class SqlSetFragment : ISqlFragment
{
    private readonly SqlCollectionFragment _collection;
    
    public IReadOnlyList<ISqlAssignmentFragment> Assignments { get; }

    public SqlSetFragment(IEnumerable<ISqlAssignmentFragment> assignments)
    {
        Assignments = assignments.ToList();
        _collection = new SqlCollectionFragment(Assignments);
    }

    public string ToSql(ISqlContext context, SqlRenderMode renderMode = SqlRenderMode.Default)
    {
        var assignmentsSql = _collection.ToSql(context, renderMode);
        
        if (context.Options.CollectionLayout == SqlCollectionLayout.Vertical)
        {
            return $"{SqlKeyword.Set}{assignmentsSql}";
        }

        return $"{SqlKeyword.Set} {assignmentsSql}";
    }
}