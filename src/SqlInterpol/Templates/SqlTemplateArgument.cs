namespace SqlInterpol;

/// <summary>
/// Represents an extracted template hole, mapping to either a named argument or a statically captured value.
/// </summary>
public readonly struct SqlTemplateArgument
{
    public bool IsCaptured { get; }
    public string? Name { get; }
    public object? CapturedValue { get; }

    public SqlTemplateArgument(string name) { IsCaptured = false; Name = name; CapturedValue = null; }
    public SqlTemplateArgument(object? capturedValue) { IsCaptured = true; Name = null; CapturedValue = capturedValue; }
}