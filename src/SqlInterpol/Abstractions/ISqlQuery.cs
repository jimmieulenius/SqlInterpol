namespace SqlInterpol;

/// <summary>
/// Represents a captured SQL query that can be built to a <see cref="SqlQueryResult"/> or embedded as a subquery.
/// </summary>
/// <seealso cref="ISqlQuery{T}"/>
public interface ISqlQuery : ISqlFragment
{
    /// <summary>Gets the ordered list of SQL segments that make up this query.</summary>
    IReadOnlyList<SqlSegment> Segments { get; }

    /// <summary>
    /// Builds the query into a <see cref="SqlQueryResult"/> containing the rendered SQL string
    /// and the dictionary of extracted parameters.
    /// </summary>
    /// <returns>The <see cref="SqlQueryResult"/> ready for execution via Dapper, EF Core, or raw ADO.NET.</returns>
    SqlQueryResult Build();
}

/// <summary>
/// A typed <see cref="ISqlQuery"/> bound to entity type <typeparamref name="T"/>, supporting
/// expression-based column indexer access for embedding as a subquery.
/// </summary>
/// <typeparam name="T">The CLR entity type whose columns can be accessed on this subquery.</typeparam>
public interface ISqlQuery<T> : ISqlQuery, ISqlEntityBase<T>
{
}