namespace SqlInterpol;

/// <summary>
/// Represents a multi-table DELETE statement, delegating dialect-specific rendering
/// to <see cref="ISqlDialect.RenderFragment"/>.
/// </summary>
public class SqlMultiTableDeleteFragment : ISqlFragment, ISqlSwappableFragment, ISqlFeatureRequirement
{
    /// <inheritdoc />
    public SqlFeature RequiredFeature => SqlFeature.MultiTableDelete;

    /// <inheritdoc />
    public string FeatureName => "Multi-Table DELETE";

    /// <summary>Gets the target table fragment (the table from which rows are deleted).</summary>
    public ISqlFragment Target { get; }

    /// <summary>Gets the FROM clause fragment (the joined source tables).</summary>
    public ISqlFragment FromClause { get; }

    /// <summary>Gets the optional WHERE clause fragment.</summary>
    public ISqlFragment? WhereClause { get; }

    /// <summary>
    /// Initializes a new <see cref="SqlMultiTableDeleteFragment"/>.
    /// </summary>
    /// <param name="target">The target table fragment.</param>
    /// <param name="fromClause">The FROM clause fragment.</param>
    /// <param name="whereClause">The optional WHERE clause fragment.</param>
    public SqlMultiTableDeleteFragment(ISqlFragment target, ISqlFragment fromClause, ISqlFragment? whereClause)
    {
        Target = target;
        FromClause = fromClause;
        WhereClause = whereClause;
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
        var mappedTarget = MapFragment(Target, entityMap, argumentGetters, arguments);
        var mappedFrom = MapFragment(FromClause, entityMap, argumentGetters, arguments);
        var mappedWhere = WhereClause != null ? MapFragment(WhereClause, entityMap, argumentGetters, arguments) : null;

        return new SqlMultiTableDeleteFragment(mappedTarget, mappedFrom, mappedWhere);
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