namespace SqlInterpol.Metadata;

public class SqlViewAttribute(string name, string? schema = null) : SqlEntityAttribute(name, schema)
{
    public override SqlEntityType Type => SqlEntityType.View;
}