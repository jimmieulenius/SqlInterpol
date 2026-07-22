namespace SqlInterpol.Execution;

/// <summary>
/// Represents an extracted template hole, mapping to either a named argument or a statically captured value.
/// </summary>
public readonly struct SqlTemplateArgument
{
    /// <summary>Gets a value indicating whether this argument represents a statically captured value.</summary>
    public bool IsCaptured { get; }
    
    /// <summary>Gets the name of the dynamic argument, if applicable.</summary>
    public string? Name { get; }
    
    /// <summary>Gets the statically captured value, if applicable.</summary>
    public object? CapturedValue { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="SqlTemplateArgument"/> struct for a named dynamic argument.
    /// </summary>
    /// <param name="name">The name of the argument.</param>
    public SqlTemplateArgument(string name) 
    { 
        IsCaptured = false; 
        Name = name; 
        CapturedValue = null; 
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="SqlTemplateArgument"/> struct for a captured static value.
    /// </summary>
    /// <param name="capturedValue">The statically captured value.</param>
    public SqlTemplateArgument(object? capturedValue) 
    { 
        IsCaptured = true; 
        Name = null; 
        CapturedValue = capturedValue; 
    }
}