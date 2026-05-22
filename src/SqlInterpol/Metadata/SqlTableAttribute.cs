namespace SqlInterpol;

/// <summary>
/// Marks a class as a SQL table entity and optionally overrides the physical table name and schema.
/// </summary>
/// <remarks>
/// <example>
/// <code>
/// [SqlTable("Products", schema: "dbo")]
/// public class Product
/// {
///     public int Id { get; set; }
///     public string Name { get; set; }
/// }
/// </code>
/// </example>
/// </remarks>
/// <param name="name">Optional physical table name override. Defaults to the class name.</param>
/// <param name="schema">Optional schema override.</param>
public class SqlTableAttribute(string? name = null, string? schema = null) : SqlEntityAttribute(name, schema)
{
    /// <inheritdoc />
    public override SqlEntityType Type => SqlEntityType.Table;
}