using SqlInterpol.Schema;

namespace SqlInterpol.Segments;

/// <summary>
/// Represents a single column projection, associating a SQL reference with a CLR property name.
/// </summary>
/// <seealso cref="ISqlProjection{T}"/>
public interface ISqlProjection : ISqlFragment
{
    /// <summary>Gets the SQL reference (column or expression) being projected.</summary>
    ISqlReference Reference { get; }

    /// <summary>Gets the CLR property name this projection maps to.</summary>
    string PropertyName { get; }
}

/// <summary>
/// Typed marker for projections bound to a specific entity type <typeparamref name="T"/>.
/// </summary>
/// <typeparam name="T">The CLR entity type this projection belongs to.</typeparam>
public interface ISqlProjection<T> : ISqlProjection
{
}