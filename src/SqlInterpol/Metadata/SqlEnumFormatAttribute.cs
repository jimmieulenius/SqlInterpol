namespace SqlInterpol;

[AttributeUsage(AttributeTargets.Property)]
public class SqlEnumFormatAttribute(SqlEnumFormat format) : Attribute
{
    public SqlEnumFormat Format { get; } = format;
}