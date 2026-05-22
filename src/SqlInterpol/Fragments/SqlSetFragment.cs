
namespace SqlInterpol;

/// <summary>
/// Renders a SET clause for UPDATE statements, joining column-value assignments
/// with layout-aware formatting.
/// </summary>
public class SqlSetFragment : ISqlFragment
{
    private readonly SqlCollectionFragment _collection;

    /// <summary>Gets the column-value assignments in this SET clause.</summary>
    public IReadOnlyList<ISqlAssignmentFragment> Assignments { get; }

    /// <summary>
    /// Initializes a new <see cref="SqlSetFragment"/> with the given assignments.
    /// </summary>
    /// <param name="assignments">The column-value assignments to include.</param>
    public SqlSetFragment(IEnumerable<ISqlAssignmentFragment> assignments)
    {
        Assignments = assignments.ToList();
        _collection = new SqlCollectionFragment(Assignments);
    }

    /// <inheritdoc />
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