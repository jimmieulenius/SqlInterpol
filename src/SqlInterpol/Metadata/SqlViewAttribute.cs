namespace SqlInterpol;

/// <summary>
/// Marks a class as a SQL view entity and optionally overrides the physical view name and schema.
/// </summary>
/// <remarks>
/// <example>
/// <code>
/// [SqlView("ActiveProducts", schema: "dbo")]
/// public class ActiveProduct
/// {
///     public int Id { get; set; }
///     public string Name { get; set; }
/// }
/// </code>
/// </example>
/// </remarks>
/// <param name="name">Optional physical view name override. Defaults to the class name.</param>
/// <param name="schema">Optional schema override.</param>
public class SqlViewAttribute(string? name = null, string? schema = null) : SqlEntityAttribute(name, schema)
{
    /// <inheritdoc />
    public override SqlEntityType Type => SqlEntityType.View;
}