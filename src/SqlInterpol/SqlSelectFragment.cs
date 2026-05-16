using SqlInterpol.Config;

namespace SqlInterpol;

public class SqlSelectFragment : ISqlFragment
{
    private readonly SqlCollectionFragment _collection;
    
    public IReadOnlyList<ISqlFragment> Columns { get; }

    public SqlSelectFragment(IEnumerable<ISqlFragment> columns)
    {
        Columns = columns.ToList();
        _collection = new SqlCollectionFragment(Columns);
    }

    public string ToSql(ISqlContext context, SqlRenderMode renderMode = SqlRenderMode.Default)
    {
        var columnsSql = _collection.ToSql(context, renderMode);
        
        if (context.Options.CollectionLayout == SqlCollectionLayout.Vertical)
        {
            return $"{SqlKeyword.Select}{columnsSql}"; 
        }

        return $"{SqlKeyword.Select} {columnsSql}"; 
    }
}