namespace SqlInterpol;

/// <summary>
/// Provides centralized utility methods for mapping SQL template placeholders to their runtime values.
/// Used by implementations of <see cref="ISqlSwappableFragment"/> to resolve columns and arguments.
/// </summary>
public static class SqlTemplateMapper
{
    /// <summary>
    /// Maps a single assignment fragment's column reference and argument placeholder to their actual runtime representations.
    /// </summary>
    /// <param name="assignment">The assignment fragment containing potential template placeholders.</param>
    /// <param name="entityMap">The dictionary mapping dummy template entity references to the actual entities being queried.</param>
    /// <param name="argumentGetters">The dictionary of pre-compiled property getters used to extract values from the argument payload.</param>
    /// <param name="arguments">The runtime payload object containing the actual parameter values.</param>
    /// <returns>A new <see cref="ISqlAssignmentFragment"/> with mapped references and resolved values.</returns>
    public static ISqlAssignmentFragment MapAssignment(
        ISqlAssignmentFragment assignment, 
        Dictionary<ISqlReference, ISqlEntityBase> entityMap, 
        IReadOnlyDictionary<string, Func<object, object?>>? argumentGetters,
        object? arguments)
    {
        ISqlReference currentRef = assignment.Reference;
        
        if (assignment.Reference is SqlColumnReference colRef && entityMap.TryGetValue(colRef.SourceReference, out var realEntity))
        {
            currentRef = new SqlColumnReference(realEntity.Reference, colRef.ColumnName, colRef.PropertyName);
        }

        object? currentValue = assignment.Value;
        
        if (assignment.Value is SqlArgumentFragment argFragment)
        {
            if (argumentGetters != null && argumentGetters.TryGetValue(argFragment.Name, out var getter))
            {
                currentValue = getter(arguments!);
            }
        }

        return new SqlAssignmentFragment(currentRef, currentValue);
    }
}