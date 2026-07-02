namespace SqlInterpol;

/// <summary>
/// Exposes a settable alias property for SQL AST nodes.
/// Implementing this allows the preprocessor to seamlessly inject dynamically resolved aliases.
/// </summary>
public interface ISqlAliasable
{
    /// <summary>
    /// Gets or sets the alias for the object.
    /// </summary>
    string? Alias { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the assigned alias is already quoted.
    /// </summary>
    bool IsAliasQuoted { get; set; }
}