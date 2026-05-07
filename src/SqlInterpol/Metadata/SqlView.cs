namespace SqlInterpol.Metadata;

public class SqlView<T>(string name, string? schema, string? alias = null)
    : SqlEntity<T>(name, schema, alias)
{
}