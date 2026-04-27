namespace SqlInterpol.Models;

public class SqlView<T>(string name, string? schema) : SqlEntity<T>(name, schema)
{
}