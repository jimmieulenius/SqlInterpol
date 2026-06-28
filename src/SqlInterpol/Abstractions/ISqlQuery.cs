namespace SqlInterpol;

/// <summary>
/// Represents a complete, top-level executable query capable of compiling into a finalized SQL statement and parameter matrix.
/// </summary>
public interface ISqlQuery : ISqlQueryFragment
{
    /// <summary>
    /// Compiles the query into its final executable SQL string and parameter mapping state.
    /// </summary>
    /// <returns>The compiled <see cref="SqlQueryResult"/> ready for execution via ADO.NET, Dapper, or EF Core.</returns>
    SqlQueryResult Build();
}

/// <summary>
/// A strongly-typed, executable <see cref="ISqlQuery"/> bound to entity model type <typeparamref name="T"/>, 
/// capable of being built directly or composed seamlessly as a subquery.
/// </summary>
/// <typeparam name="T">The CLR entity model type whose columns are exposed by this subquery context.</typeparam>
public interface ISqlQuery<T> : ISqlQuery, ISqlQueryFragment<T>
{
}