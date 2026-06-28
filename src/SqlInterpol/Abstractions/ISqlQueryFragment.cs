namespace SqlInterpol;

/// <summary>
/// Represents an ad-hoc query component, clause expression, or subquery within the AST rendering tree.
/// </summary>
public interface ISqlQueryFragment : ISqlFragment, ISqlSegmentContainer
{
    /// <summary>
    /// Gets the ordered list of SQL segments that make up this query fragment snapshot.
    /// </summary>
    IReadOnlyList<SqlSegment> Segments { get; }

    /// <summary>
    /// Gets or sets a value indicating whether the outer structural parentheses should be omitted during rendering.
    /// </summary>
    bool ExcludeParentheses { get; set; }
}

/// <summary>
/// A strongly-typed <see cref="ISqlQueryFragment"/> bound to a primary entity type <typeparamref name="T"/>, 
/// supporting expression-based property column selectors when embedded as a clause.
/// </summary>
/// <typeparam name="T">The CLR entity model type whose columns are exposed by this subquery fragment context.</typeparam>
public interface ISqlQueryFragment<T> : ISqlQueryFragment, ISqlEntityBase<T>
{
}