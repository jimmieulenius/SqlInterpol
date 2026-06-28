namespace SqlInterpol;

/// <summary>
/// Exposes a settable alias property for SQL AST nodes.
/// Implementing this allows the preprocessor to seamlessly inject dynamically resolved aliases.
/// </summary>
public interface ISqlAliasable
{
    string? Alias { get; set; }
}