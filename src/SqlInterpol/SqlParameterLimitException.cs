namespace SqlInterpol;

/// <summary>
/// Thrown when a single query attempts to generate more parameters than the active SQL dialect supports.
/// </summary>
public class SqlParameterLimitException(int limit, int requested) : Exception($"The query attempted to generate {requested} parameters, which exceeds the configured maximum limit of {limit}. " +
               $"For unbounded bulk operations, use the db.BulkInsert() or db.BulkUpdate() extension methods to automatically chunk batches. " +
               $"For massive IN (...) clauses, consider using Temporary Tables, JSON array parameters, or splitting the query.")
{
}