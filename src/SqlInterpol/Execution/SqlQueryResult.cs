namespace SqlInterpol;

/// <summary>
/// The immutable result of building a query, containing the final
/// SQL string and the dictionary of extracted parameters ready for execution.
/// </summary>
/// <param name="Sql">The rendered SQL string with named parameter placeholders (e.g., <c>@p0</c>).</param>
/// <param name="Parameters">The named parameters extracted from interpolated values, keyed by placeholder name.</param>
public record SqlQueryResult(string Sql, IReadOnlyDictionary<string, object?> Parameters);