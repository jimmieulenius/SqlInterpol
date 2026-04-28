namespace SqlInterpol.Metadata;

public class SqlTable<T>(string name, string? schema) : SqlEntity<T>(name, schema)
{
}