using System.Linq.Expressions;
using SqlInterpol.Parsing;

namespace SqlInterpol;

/// <summary>
/// Extension methods for <see cref="ISqlEntityBase{T}"/> that provide aliasing and
/// strongly-typed <c>ORDER BY</c> fragment construction.
/// </summary>
public static class SqlEntityExtensions
{
    /// <summary>
    /// Assigns a SQL alias to the entity reference, overriding any previously set alias.
    /// The alias will be quoted with the active dialect's identifier quoting when rendered.
    /// </summary>
    /// <typeparam name="T">The CLR model type mapped to this entity.</typeparam>
    /// <param name="entity">The entity whose alias is being set.</param>
    /// <param name="alias">The alias to assign (e.g. <c>"p"</c> renders as <c>[p]</c> or <c>"p"</c> depending on dialect).</param>
    /// <returns>The same <paramref name="entity"/> instance for method chaining.</returns>
    public static ISqlEntityBase<T> As<T>(this ISqlEntityBase<T> entity, string alias)
    {
        if (entity.Reference != null)
        {
            entity.Reference.Alias = alias;
            entity.Reference.IsAliasQuoted = true;
        }
        
        return entity;
    }

    // TODO (v2.0): Remove when deleting old lambda syntax
    [Obsolete("Use standard SQL syntax with the new zero-allocation variables (e.g., ORDER BY {p.Id} ASC)")]
    public static ISqlOrderFragment OrderBy<T>(
        this ISqlEntityBase<T> entity,
        string propertyName, 
        SqlOrderDirection? direction = null)
    {
        var meta = SqlMetadataRegistry.GetMetadata<T>();
        
        var columnMap = meta.Columns.FirstOrDefault(c => 
            c.Key.Name.Equals(propertyName, StringComparison.OrdinalIgnoreCase));

        if (columnMap.Key == null)
        {
            throw new ArgumentException($"Property '{propertyName}' not found on '{typeof(T).Name}'.");
        }

        return new SqlOrderFragment(entity, columnMap.Value, direction);
    }

    // TODO (v2.0): Remove when deleting old lambda syntax
    [Obsolete("Use standard SQL syntax with the new zero-allocation variables (e.g., ORDER BY {p.Id} ASC)")]
    public static ISqlOrderFragment OrderBy<T>(
        this ISqlEntityBase<T> entity,
        Expression<Func<T, object?>> expression, 
        SqlOrderDirection? direction = null)
    {
        var memberInfo = SqlExpressionHelper.GetProperty(expression);
        var meta = SqlMetadataRegistry.GetMetadata<T>();

        if (!meta.Columns.TryGetValue(memberInfo, out string? physicalName))
        {
            throw new ArgumentException($"Property '{memberInfo.Name}' not mapped.");
        }

        return new SqlOrderFragment(entity, physicalName, direction);
    }
}