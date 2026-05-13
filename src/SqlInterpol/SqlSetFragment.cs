using System.Collections.Generic;
using SqlInterpol.Config;

namespace SqlInterpol;

public class SqlSetFragment : ISqlFragment
{
    private readonly SqlCollectionFragment _collection;

    public SqlSetFragment(IEnumerable<ISqlAssignmentFragment> assignments)
    {
        _collection = new SqlCollectionFragment(assignments);
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