namespace SqlInterpol;

/// <summary>
/// Represents a SQL view entity mapped to CLR type <typeparamref name="T"/>.
/// </summary>
/// <typeparam name="T">The CLR type decorated with <see cref="SqlViewAttribute"/>.</typeparam>
/// <param name="name">The physical view name.</param>
/// <param name="schema">The schema, or <see langword="null"/> for the default schema.</param>
/// <param name="alias">The SQL alias for use in query clauses, or <see langword="null"/> to use the type name.</param>
public class SqlView<T>(string name, string? schema, string? alias = null)
    : SqlEntity<T>(name, schema, alias)
{
}