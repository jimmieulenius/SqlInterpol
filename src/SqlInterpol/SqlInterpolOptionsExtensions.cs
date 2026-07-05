namespace SqlInterpol;

public static class SqlInterpolOptionsExtensions
{
    /// <summary>
    /// Registers a third-party extension into the engine.
    /// </summary>
    /// <param name="extension">The extension instance to register.</param>
    /// <returns>The current options instance for fluent chaining.</returns>
    public static SqlInterpolOptions AddExtension(this SqlInterpolOptions options, ISqlExtension extension)
    {
        extension.Register(options);
        
        return options;
    }
}