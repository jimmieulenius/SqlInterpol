namespace SqlInterpol;

public record SqlQueryResult(string Sql, IReadOnlyDictionary<string, object?> Parameters);