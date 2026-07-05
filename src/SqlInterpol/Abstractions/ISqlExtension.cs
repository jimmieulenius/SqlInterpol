namespace SqlInterpol;

/// <summary>
/// Represents a third-party extension or plugin for the SqlInterpol engine.
/// Implement this interface to bundle custom keywords, lexical rules, and AST rewriters.
/// </summary>
public interface ISqlExtension
{
    /// <summary>
    /// Registers the extension's components into the provided options configuration.
    /// </summary>
    /// <param name="options">The configuration object to modify.</param>
    void Register(SqlInterpolOptions options);
}