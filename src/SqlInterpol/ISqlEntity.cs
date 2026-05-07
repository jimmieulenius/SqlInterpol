namespace SqlInterpol;

public interface ISqlEntity : ISqlEntityBase
{
    string Name { get; }
    string? Schema { get; }
}

public interface ISqlEntity<T> : ISqlEntity, ISqlEntityBase<T>
{
}