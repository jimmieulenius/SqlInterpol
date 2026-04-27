namespace SqlInterpol.Models;

public class SqlTable<T>(string name, string? schema) : SqlEntity<T>(name, schema)
{
}