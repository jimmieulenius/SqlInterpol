namespace SqlInterpol;

/// <summary>
/// Thrown when a single query attempts to generate more parameters than the active SQL dialect natively supports.
/// </summary>
/// <param name="limit">The maximum number of parameters allowed by the configured dialect.</param>
/// <param name="requested">The number of parameters the query actually attempted to generate.</param>
public class SqlParameterLimitException(int limit, int requested) 
    : Exception($"The query attempted to generate {requested} parameters, which exceeds the configured maximum limit of {limit}. " +
                $"For unbounded bulk operations, use the db.BulkInsert() or db.BulkUpdate() extension methods to automatically chunk batches. " +
                $"For massive IN (...) clauses, consider using Temporary Tables, JSON array parameters, or splitting the query.");