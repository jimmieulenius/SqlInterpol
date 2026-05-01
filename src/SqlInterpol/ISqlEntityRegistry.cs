namespace SqlInterpol;

public interface ISqlEntityRegistry
{
    ISqlEntity<T> RegisterEntity<T>(string? name = null, string? schema = null);
    
    // We could eventually add things like:
    // IEnumerable<ISqlEntity> GetRegisteredEntities();
}