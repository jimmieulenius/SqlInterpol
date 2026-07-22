using SqlInterpol.Segments;

namespace SqlInterpol.Schema;

/// <summary>
/// Represents a registered database entity (like a table or view) within a SQL query.
/// </summary>
public interface ISqlEntity : ISqlFragment
{
    /// <summary>
    /// Gets the CLR model type that this entity maps to.
    /// </summary>
    System.Type ModelType { get; }
}

/// <summary>
/// Represents a registered database entity bound to a specific CLR model type.
/// </summary>
/// <typeparam name="T">The CLR model type.</typeparam>
public interface ISqlEntity<T> : ISqlEntity
{
}