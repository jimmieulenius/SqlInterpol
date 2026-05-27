namespace SqlInterpol;

/// <summary>
/// Represents a named placeholder for a runtime parameter in a pre-compiled SQL template.
/// </summary>
public class SqlArgumentFragment : ISqlFragment
{
    public string Name { get; }

    public SqlArgumentFragment(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Template argument name cannot be null or empty.", nameof(name));
            
        Name = name;
    }

    /// <inheritdoc />
    public string ToSql(ISqlContext context, SqlRenderMode mode = SqlRenderMode.Default)
    {
        throw new InvalidOperationException(
            $"The template argument '{Name}' was not supplied at runtime. " +
            "Make sure you pass an arguments object containing this property when appending the template or building the query.");
    }
}