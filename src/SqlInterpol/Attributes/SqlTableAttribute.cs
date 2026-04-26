namespace SqlInterpol.Attributes;

[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public class SqlTableAttribute : Attribute
{
    public string? SchemaName { get; set; }
    public string? TableName { get; set; }

    /// <summary>
    /// Define SQL table mapping for a class with optional parameters
    /// </summary>
    /// <param name="tableName">Table name (if null, uses class name)</param>
    /// <param name="schemaName">Database schema (e.g., "dbo")</param>
    public SqlTableAttribute(string? tableName = null, string? schemaName = null)
    {
        TableName = tableName;
        SchemaName = schemaName;
    }
}