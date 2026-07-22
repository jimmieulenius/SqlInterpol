using SqlInterpol.Configuration;
using SqlInterpol.Pipeline;

namespace SqlInterpol;

/// <summary>
/// Provides extension methods for configuring global SQL generation options.
/// </summary>
public static class SqlInterpolOptionsExtensions
{
    /// <summary>
    /// Registers a third-party extension into the engine.
    /// </summary>
    /// <param name="options">The options instance to configure.</param>
    /// <param name="extension">The extension instance to register.</param>
    /// <returns>The current options instance for fluent chaining.</returns>
    public static SqlInterpolOptions AddExtension(this SqlInterpolOptions options, ISqlExtension extension)
    {
        extension.Register(options);
        return options;
    }
}