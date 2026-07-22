using System.Reflection;

namespace SqlInterpol.Schema;

/// <summary>
/// Holds the cached reflection and mapping metadata for a database entity.
/// </summary>
/// <param name="Name">The physical name of the table or view.</param>
/// <param name="Schema">The database schema the entity belongs to, if any.</param>
/// <param name="Type">The structural type of the entity (Table or View).</param>
/// <param name="Columns">A dictionary mapping CLR properties to their physical column names.</param>
public record SqlEntityMetadata(
    string Name, 
    string? Schema, 
    SqlEntityType Type, 
    IReadOnlyDictionary<PropertyInfo, string> Columns);