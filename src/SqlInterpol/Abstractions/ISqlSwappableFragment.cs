namespace SqlInterpol;

/// <summary>
/// Defines a structural SQL fragment (AST branch node) that is capable of recursively swapping 
/// its internal template placeholders with actual runtime references and data.
/// </summary>
public interface ISqlSwappableFragment : ISqlFragment
{
    /// <summary>
    /// Evaluates internal placeholders and returns a new fragment containing the resolved runtime references and arguments.
    /// </summary>
    /// <param name="entityMap">A dictionary mapping dummy template entity references to the actual entities being queried.</param>
    /// <param name="argumentGetters">A dictionary of pre-compiled property getters used to extract values from the argument payload.</param>
    /// <param name="arguments">The runtime payload object containing the actual parameter values.</param>
    /// <returns>A new <see cref="ISqlFragment"/> with all placeholders resolved.</returns>
    ISqlFragment Swap(
        Dictionary<ISqlReference, ISqlEntityBase> entityMap, 
        IReadOnlyDictionary<string, Func<object, object?>>? argumentGetters, 
        object? arguments);
}