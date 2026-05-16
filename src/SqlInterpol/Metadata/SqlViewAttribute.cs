namespace SqlInterpol.Metadata;

public class SqlViewAttribute(string? name = null, string? schema = null) : SqlEntityAttribute(name, schema)
{
    public override SqlEntityType Type => SqlEntityType.View;
}