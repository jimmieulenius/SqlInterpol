namespace SqlInterpol;

/// <summary>
/// Specifies the set operator used to combine the results of two SQL queries.
/// </summary>
public enum SqlSetOperator
{
    /// <summary>Combines results and removes duplicate rows (<c>UNION</c>).</summary>
    Union,
    /// <summary>Combines results and retains all rows including duplicates (<c>UNION ALL</c>).</summary>
    UnionAll,
    /// <summary>Returns only rows that appear in both result sets (<c>INTERSECT</c>).</summary>
    Intersect,
    /// <summary>Returns rows from the first result set that do not appear in the second (<c>EXCEPT</c> / <c>MINUS</c>).</summary>
    Except
}