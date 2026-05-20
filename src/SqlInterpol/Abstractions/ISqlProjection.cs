namespace SqlInterpol;

public interface ISqlProjection : ISqlFragment
{
    ISqlReference Reference { get; }
    string PropertyName { get; }
}

public interface ISqlProjection<T> : ISqlProjection
{
}