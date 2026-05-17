using SqlInterpol;
using SqlInterpol.Config;

public class SqlSelectFragment : ISqlFragment
{
    private readonly SqlCollectionFragment _collection;
    private readonly string _keyword;
    
    public IReadOnlyList<ISqlFragment> Columns { get; }

    public SqlSelectFragment(IEnumerable<ISqlFragment> columns, bool isDistinct = false)
    {
        Columns = columns.ToList();
        _collection = new SqlCollectionFragment(Columns);
        _keyword = isDistinct ? $"{SqlKeyword.Select} {SqlKeyword.Distinct}" : SqlKeyword.Select;
    }

    public string ToSql(ISqlContext context, SqlRenderMode renderMode = SqlRenderMode.Default)
    {
        var columnsSql = _collection.ToSql(context, renderMode);
        
        if (context.Options.CollectionLayout == SqlCollectionLayout.Vertical)
        {
            return $"{_keyword}{columnsSql}"; 
        }

        return $"{_keyword} {columnsSql}"; 
    }
}