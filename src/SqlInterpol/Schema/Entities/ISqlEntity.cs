namespace SqlInterpol.Schema;

/// <summary>
/// Represents a fully concrete, mapped SQL entity.
/// </summary>
public interface ISqlEntity : ISqlEntityBase
{
}

/// <summary>
/// Represents a fully concrete SQL entity bound to a specific CLR model type.
/// </summary>
/// <typeparam name="T">The CLR model type.</typeparam>
public interface ISqlEntity<T> : ISqlEntityBase<T>, ISqlEntity
{
}