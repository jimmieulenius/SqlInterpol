namespace SqlInterpol.Attributes;

[AttributeUsage(AttributeTargets.Property)]
public class SqlColumnAttribute : Attribute
{
    public string? ColumnName { get; }

    public SqlColumnAttribute()
    {
    }

    public SqlColumnAttribute(string columnName)
    {
        ColumnName = columnName;
    }
}