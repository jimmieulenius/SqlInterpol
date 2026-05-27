namespace SqlInterpol;

/// <summary>
/// Defines a template marker that instructs the SQL parser to expand a Data Transfer Object (DTO) 
/// into structural SQL fragments (such as SET assignments or INSERT values) ahead of time.
/// </summary>
public interface ISqlExpandable
{
    /// <summary>
    /// Gets the runtime type of the DTO to expand.
    /// </summary>
    Type DtoType { get; }

    /// <summary>
    /// Gets a read-only set of property names designated as primary keys. 
    /// The parser uses this context to automatically route properties (e.g., excluding these keys from a SET clause).
    /// </summary>
    IReadOnlySet<string> KeyProperties { get; }
}