namespace SqlInterpol.Metadata;

public class SqlView<T>(string name, string? schema) : SqlEntity<T>(name, schema)
{
}