using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace SqlInterpol;

/// <summary>
/// Holds the resolved metadata for a SQL entity type: physical name, schema, kind, and column mappings.
/// </summary>
/// <param name="Name">The physical table or view name.</param>
/// <param name="Schema">The schema, or <see langword="null"/> for the default schema.</param>
/// <param name="Type">Whether this entity is a <see cref="SqlEntityType.Table"/>, <see cref="SqlEntityType.View"/>, or subquery.</param>
/// <param name="Columns">
/// A map from each <see cref="MemberInfo"/> to its physical column name,
/// respecting any <see cref="SqlColumnAttribute"/> override.
/// </param>
public record SqlEntityMetadata(
    string Name, 
    string? Schema, 
    SqlEntityType Type,
    IReadOnlyDictionary<MemberInfo, string> Columns
)
{
    /// <summary>
    /// A pre-sorted list of columns by member name to eliminate O(N log N) 
    /// OrderBy allocations in the hot path during SELECT expansions.
    /// </summary>
    public IReadOnlyList<KeyValuePair<MemberInfo, string>> SortedColumns { get; } = 
        Columns.OrderBy(c => c.Key.Name).ToArray();
}