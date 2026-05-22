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

    /// <summary>
    /// Creates an <c>ORDER BY</c> fragment for the column mapped to <paramref name="propertyName"/> on <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T">The CLR model type mapped to this entity.</typeparam>
    /// <param name="entity">The entity that owns the column.</param>
    /// <param name="propertyName">The case-insensitive CLR property name to order by.</param>
    /// <param name="direction">The sort direction; defaults to the database default (<c>ASC</c>) when <see langword="null"/>.</param>
    /// <returns>An <see cref="ISqlOrderFragment"/> that can be interpolated into an <c>ORDER BY</c> clause.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="propertyName"/> is not mapped on <typeparamref name="T"/>.</exception>
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

    /// <summary>
    /// Creates an <c>ORDER BY</c> fragment for the column mapped to the property selected by <paramref name="expression"/>.
    /// </summary>
    /// <typeparam name="T">The CLR model type mapped to this entity.</typeparam>
    /// <param name="entity">The entity that owns the column.</param>
    /// <param name="expression">A lambda expression that selects the property to order by (e.g. <c>x =&gt; x.Name</c>).</param>
    /// <param name="direction">The sort direction; defaults to the database default (<c>ASC</c>) when <see langword="null"/>.</param>
    /// <returns>An <see cref="ISqlOrderFragment"/> that can be interpolated into an <c>ORDER BY</c> clause.</returns>
    /// <exception cref="ArgumentException">Thrown when the selected property is not mapped on <typeparamref name="T"/>.</exception>
    public static ISqlOrderFragment OrderBy<T>(
        this ISqlEntityBase<T> entity,
        Expression<Func<T, object?>> expression, 
        SqlOrderDirection? direction = null)
    {
        var memberInfo = SqlExpressionHelper.GetMember(expression);
        var meta = SqlMetadataRegistry.GetMetadata<T>();

        if (!meta.Columns.TryGetValue(memberInfo, out string? physicalName))
        {
            throw new ArgumentException($"Property '{memberInfo.Name}' not mapped.");
        }

        return new SqlOrderFragment(entity, physicalName, direction);
    }
}