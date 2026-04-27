using SqlInterpol.Abstractions;

namespace SqlInterpol.Models;

public abstract class SqlReference(ISqlProjection parent) : ISqlReference
{
    public ISqlProjection Parent { get; } = parent;
    public string? Alias { get; set; }

    // Every specific reference (Table vs Column) 
    // must decide how its 'Pointer' looks.
    public abstract string ToSql(SqlContext context);
}