using SqlInterpol.Configuration;

namespace SqlInterpol.Segments;

/// <summary>
/// Represents a named placeholder for a runtime parameter in a pre-compiled SQL template.
/// </summary>
/// <param name="name">The name of the template argument.</param>
public class SqlArgumentFragment(string name) : ISqlFragment
{
    /// <summary>Gets the name of the template argument.</summary>
    public string Name { get; } = string.IsNullOrWhiteSpace(name) 
        ? throw new ArgumentException("Template argument name cannot be null or empty.", nameof(name)) 
        : name;

    /// <inheritdoc />
    public string ToSql(ISqlContext context, SqlRenderMode mode = SqlRenderMode.Default)
    {
        throw new InvalidOperationException(
            $"The template argument '{Name}' was not supplied at runtime. " +
            "Make sure you pass an arguments object containing this property when appending the template or building the query.");
    }
}