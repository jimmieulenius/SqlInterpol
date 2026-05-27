namespace SqlInterpol;

/// <summary>
/// Renders a SELECT (or SELECT DISTINCT) clause, joining projected columns with
/// layout-aware formatting.
/// </summary>
public class SqlSelectFragment : ISqlFragment, ISqlSwappableFragment
{
    private readonly SqlCollectionFragment _collection;
    private readonly string _keyword;

    /// <summary>Gets the list of projected column fragments.</summary>
    public IReadOnlyList<ISqlFragment> Columns { get; }

    /// <summary>
    /// Initializes a new <see cref="SqlSelectFragment"/>.
    /// </summary>
    /// <param name="columns">The column projections to include in the SELECT list.</param>
    /// <param name="isDistinct">When <see langword="true"/>, emits <c>SELECT DISTINCT</c>.</param>
    public SqlSelectFragment(IEnumerable<ISqlFragment> columns, bool isDistinct = false)
    {
        Columns = columns.ToList();
        _collection = new SqlCollectionFragment(Columns);
        _keyword = isDistinct ? $"{SqlKeyword.Select} {SqlKeyword.Distinct}" : SqlKeyword.Select;
    }

    /// <inheritdoc />
    public string ToSql(ISqlContext context, SqlRenderMode renderMode = SqlRenderMode.Default)
    {
        var columnsSql = _collection.ToSql(context, renderMode);
        
        if (context.Options.CollectionLayout == SqlCollectionLayout.Vertical)
        {
            return $"{_keyword}{columnsSql}"; 
        }

        return $"{_keyword} {columnsSql}"; 
    }

    /// <inheritdoc />
    public ISqlFragment Swap(
        Dictionary<ISqlReference, ISqlEntityBase> entityMap, 
        IReadOnlyDictionary<string, Func<object, object?>>? argumentGetters, 
        object? arguments)
    {
        var mappedColumns = new List<ISqlFragment>(Columns.Count);
        
        foreach (var col in Columns)
        {
            if (col is SqlColumnReferenceBase colRef && entityMap.TryGetValue(colRef.SourceReference, out var realEntity))
            {
                mappedColumns.Add(colRef is SqlRawColumnReference 
                    ? (SqlColumnReferenceBase)new SqlRawColumnReference(realEntity.Reference, colRef.ColumnName)
                    : new SqlColumnReference(realEntity.Reference, colRef.ColumnName, colRef.PropertyName));
            }
            else if (col is ISqlSwappableFragment swappable)
            {
                mappedColumns.Add(swappable.Swap(entityMap, argumentGetters, arguments));
            }
            else
            {
                mappedColumns.Add(col);
            }
        }

        bool isDistinct = _keyword != SqlKeyword.Select;
        return new SqlSelectFragment(mappedColumns, isDistinct);
    }
}