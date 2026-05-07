namespace SqlInterpol.Metadata;

public class SqlTable<T>(string name, string? schema, string? alias = null) 
    : SqlEntity<T>(name, schema, alias)
{
}