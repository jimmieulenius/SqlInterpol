namespace SqlInterpol;

/// <summary>
/// Maps a CLR property to a specific physical column name in SQL queries.
/// When absent, the property name is used as the column name.
/// </summary>
/// <remarks>
/// <example>
/// <code>
/// public class Product
/// {
///     [SqlColumn("product_name")]
///     public string Name { get; set; }
/// }
/// </code>
/// </example>
/// </remarks>
[AttributeUsage(AttributeTargets.Property)]
public class SqlColumnAttribute : Attribute
{
    /// <summary>Gets the physical column name override, or <see langword="null"/> to use the property name.</summary>
    public string? Name { get; }

    /// <summary>
    /// Marks the property as a mapped column, using the property name as the column name.
    /// </summary>
    public SqlColumnAttribute()
    {
    }

    /// <summary>
    /// Maps the property to the specified physical column name.
    /// </summary>
    /// <param name="name">The physical column name to use in generated SQL.</param>
    public SqlColumnAttribute(string name)
    {
        Name = name;
    }
}