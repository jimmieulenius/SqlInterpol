namespace SqlInterpol.Configuration;

/// <summary>
/// Represents a third-party extension or plugin for the SqlInterpol engine.
/// Implement this interface and use <see cref="SqlExtensionRegistry.Register"/> or a
/// <c>[ModuleInitializer]</c> to bundle custom keywords, lexical rules, and segment rewriters
/// into the default pipeline.
/// </summary>
public interface ISqlExtension
{
    /// <summary>
    /// Registers the extension's components into the provided options configuration.
    /// </summary>
    /// <param name="options">The <see cref="SqlInterpolOptions"/> instance to augment.</param>
    void Register(SqlInterpolOptions options);
}