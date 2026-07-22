namespace SqlInterpol.Segments;

/// <summary>
/// Specifies the sort direction for an <c>ORDER BY</c> clause.
/// </summary>
public enum SqlOrderDirection
{
    /// <summary>Sorts rows in ascending order (<c>ASC</c>). This is the default SQL sort direction.</summary>
    Asc,
    
    /// <summary>Sorts rows in descending order (<c>DESC</c>).</summary>
    Desc
}