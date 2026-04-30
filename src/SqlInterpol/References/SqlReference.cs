using SqlInterpol.Config;

namespace SqlInterpol.References;

public abstract class SqlReference(ISqlEntity parent) : ISqlReference
{
    public ISqlEntity Source { get; } = parent;
    public string? Alias { get; set; }

    // Every specific reference (Table vs Column) 
    // must decide how its 'Pointer' looks.
    public abstract string ToSql(SqlContext context);

    public override string ToString() => $"[SqlFragment: {GetType().Name}]";
}