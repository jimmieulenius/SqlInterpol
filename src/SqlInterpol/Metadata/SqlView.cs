namespace SqlInterpol;

public class SqlView<T>(string name, string? schema, string? alias = null)
    : SqlEntity<T>(name, schema, alias)
{
}