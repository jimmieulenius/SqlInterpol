namespace SqlInterpol.Metadata;

public class SqlTableAttribute(string name, string? schema = null) : SqlEntityAttribute(name, schema)
{
    public override SqlEntityType Type => SqlEntityType.Table;
}