namespace SqlInterpol;

public interface ISqlAssignmentFragment : ISqlFragment
{
    ISqlReference Reference { get; }
    object? Value { get; }
}