namespace SqlInterpol;

public interface ISqlEntityRegistry
{
    ISqlEntity<T> RegisterEntity<T>(string? name = null, string? schema = null);
}