namespace SqlInterpol;

/// <summary>
/// Represents a set operation (UNION, INTERSECT, EXCEPT) combining two query fragments,
/// delegating dialect-specific rendering to <see cref="ISqlDialect.RenderFragment"/>.
/// </summary>
public class SqlSetOperationFragment : ISqlFragment, ISqlSwappableFragment
{
    /// <summary>Gets the left-hand query fragment.</summary>
    public ISqlFragment Left { get; }

    /// <summary>Gets the right-hand query fragment.</summary>
    public ISqlFragment Right { get; }

    /// <summary>Gets the set operator to apply between <see cref="Left"/> and <see cref="Right"/>.</summary>
    public SqlSetOperator Operator { get; }

    /// <summary>
    /// Initializes a new <see cref="SqlSetOperationFragment"/>.
    /// </summary>
    /// <param name="left">The left-hand query fragment.</param>
    /// <param name="right">The right-hand query fragment.</param>
    /// <param name="setOperator">The set operator to apply.</param>
    public SqlSetOperationFragment(ISqlFragment left, ISqlFragment right, SqlSetOperator setOperator)
    {
        Left = left;
        Right = right;
        Operator = setOperator;
    }

    /// <inheritdoc />
    public string ToSql(ISqlContext context, SqlRenderMode renderMode = SqlRenderMode.Default)
    {
        return context.Dialect.RenderFragment(this, context);
    }

    /// <inheritdoc />
    public ISqlFragment Swap(
        Dictionary<ISqlReference, ISqlEntityBase> entityMap, 
        IReadOnlyDictionary<string, Func<object, object?>>? argumentGetters, 
        object? arguments)
    {
        var mappedLeft = MapFragment(Left, entityMap, argumentGetters, arguments);
        var mappedRight = MapFragment(Right, entityMap, argumentGetters, arguments);

        return new SqlSetOperationFragment(mappedLeft, mappedRight, Operator);
    }

    private static ISqlFragment MapFragment(
        ISqlFragment fragment, 
        Dictionary<ISqlReference, ISqlEntityBase> entityMap, 
        IReadOnlyDictionary<string, Func<object, object?>>? argumentGetters, 
        object? arguments)
    {
        if (fragment is ISqlSwappableFragment swappable)
        {
            return swappable.Swap(entityMap, argumentGetters, arguments);
        }
        
        if (fragment is ISqlEntityBase entity && entityMap.TryGetValue(entity.Reference, out var realEntity))
        {
            return realEntity;
        }

        if (fragment is SqlColumnReference colRef && entityMap.TryGetValue(colRef.SourceReference, out var realSource))
        {
            return new SqlColumnReference(realSource.Reference, colRef.ColumnName, colRef.PropertyName);
        }

        return fragment;
    }
}