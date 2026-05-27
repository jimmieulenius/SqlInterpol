namespace SqlInterpol;

/// <summary>
/// Renders a single-row <c>INSERT INTO ... (cols) VALUES (...)</c> statement
/// for the given entity, generating parameters for each assignment.
/// </summary>
/// <param name="entity">The entity whose table is used as the INSERT target.</param>
/// <param name="assignments">The column-value assignments to insert.</param>
public class SqlInsertFragment(ISqlEntityBase entity, IEnumerable<ISqlAssignmentFragment> assignments) 
    : ISqlFragment, ISqlParameterGenerator, ISqlSwappableFragment
{
    /// <inheritdoc />
    public void GenerateParameters(ISqlContext context)
    {
        foreach (var assignment in assignments)
        {
            if (assignment is ISqlParameterGenerator generator)
                generator.GenerateParameters(context);
        }
    }

    /// <inheritdoc />
    public string ToSql(ISqlContext context, SqlRenderMode mode = SqlRenderMode.Default)
    {
        string intoSql = $"{SqlKeyword.Insert} {SqlKeyword.Into} {entity.Declaration.ToSql(context)}";
        var valuesFragment = new SqlInsertValuesFragment(assignments);
        
        return $"{intoSql}{Environment.NewLine}{valuesFragment.ToSql(context)}";
    }

    /// <inheritdoc />
    public ISqlFragment Swap(
        Dictionary<ISqlReference, ISqlEntityBase> entityMap, 
        IReadOnlyDictionary<string, Func<object, object?>>? argumentGetters, 
        object? arguments)
    {
        // 1. Swap the entity (Target Table)
        ISqlEntityBase mappedEntity = entity;
        if (entityMap.TryGetValue(entity.Reference, out var realEntity))
        {
            mappedEntity = realEntity;
        }

        // 2. Swap the assignments
        var mappedAssignments = new List<ISqlAssignmentFragment>();
        foreach (var assignment in assignments)
        {
            mappedAssignments.Add(SqlTemplateMapper.MapAssignment(assignment, entityMap, argumentGetters, arguments));
        }

        return new SqlInsertFragment(mappedEntity, mappedAssignments);
    }
}