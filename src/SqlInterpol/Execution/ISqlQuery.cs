using SqlInterpol.Configuration;
using SqlInterpol.Schema;
using SqlInterpol.Segments;

namespace SqlInterpol;

/// <summary>
/// Represents a captured SQL query holding a stateless segment list ready for contextual rendering.
/// </summary>
public interface ISqlQuery : ISqlFragment
{
    /// <summary>
    /// Gets the read-only list of segments that make up this query.
    /// </summary>
    IReadOnlyList<SqlSegment> Segments { get; }

    /// <summary>
    /// Gets or sets a value indicating whether to exclude the surrounding parentheses when rendering the query.
    /// </summary>
    bool ExcludeParentheses { get; set; }
}

/// <summary>
/// Represents a typed SQL subquery scope bound to a primary entity model type.
/// </summary>
/// <typeparam name="T">The model type representing the query projection.</typeparam>
public interface ISqlQuery<T> : ISqlQuery, ISqlEntityBase<T>
{
}