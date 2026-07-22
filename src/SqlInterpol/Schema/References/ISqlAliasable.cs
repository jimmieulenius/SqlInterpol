namespace SqlInterpol.Schema;

/// <summary>
/// Represents a SQL fragment or reference that can have an explicit alias assigned to it.
/// </summary>
public interface ISqlAliasable
{
    /// <summary>
    /// Gets or sets the explicit alias to use for this object in the rendered SQL.
    /// </summary>
    string? Alias { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the alias should be explicitly wrapped in 
    /// dialect-specific identifier quotes (e.g., <c>"MyAlias"</c>).
    /// </summary>
    bool IsAliasQuoted { get; set; }
}