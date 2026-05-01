using SqlInterpol.Config;

namespace SqlInterpol.References;

public abstract class SqlReference(ISqlFragment parent) : ISqlReference
{
    public ISqlFragment Source { get; } = parent;
    public string? Alias { get; set; }

    public abstract string ToSql(SqlContext context, SqlRenderMode mode = SqlRenderMode.Default);
}