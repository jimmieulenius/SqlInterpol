namespace SqlInterpol;

/// <summary>
/// Marker interface for SQL fragments that represent an ORDER BY expression.
/// </summary>
/// <remarks>
/// Implement this interface on custom ORDER BY fragments so that the renderer
/// can distinguish ordering clauses from other <see cref="ISqlFragment"/> values.
/// </remarks>
public interface ISqlOrderFragment : ISqlFragment
{
}