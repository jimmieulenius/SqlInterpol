namespace SqlInterpol;

/// <summary>
/// Registers and tracks SQL entities (tables and views) for use within a <see cref="SqlBuilder"/> scope.
/// </summary>
public interface ISqlEntityRegistry
{
    /// <summary>
    /// Registers a SQL entity mapped to CLR type <typeparamref name="T"/>, with optional overrides
    /// for name, schema, and alias.
    /// </summary>
    /// <typeparam name="T">The CLR type decorated with <see cref="SqlEntityAttribute"/> or <see cref="SqlTableAttribute"/>.</typeparam>
    /// <param name="name">Overrides the physical table or view name. When <see langword="null"/>, the name is inferred from metadata.</param>
    /// <param name="schema">Overrides the schema. When <see langword="null"/>, the schema is inferred from metadata.</param>
    /// <param name="alias">Overrides the SQL alias. When <see langword="null"/>, an alias is auto-generated.</param>
    /// <returns>The registered <see cref="ISqlEntity{T}"/> ready for use in interpolated SQL strings.</returns>
    ISqlEntity<T> RegisterEntity<T>(string? name = null, string? schema = null, string? alias = null);
}