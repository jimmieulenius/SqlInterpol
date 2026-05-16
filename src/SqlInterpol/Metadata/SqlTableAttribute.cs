namespace SqlInterpol.Metadata;

public class SqlTableAttribute(string? name = null, string? schema = null) : SqlEntityAttribute(name, schema)
{
    public override SqlEntityType Type => SqlEntityType.Table;
}