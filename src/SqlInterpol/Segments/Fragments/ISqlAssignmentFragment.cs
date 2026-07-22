using SqlInterpol.Schema;

namespace SqlInterpol.Segments;

public interface ISqlAssignmentFragment : ISqlFragment
{
    ISqlReference Reference { get; }
    object? Value { get; }
}