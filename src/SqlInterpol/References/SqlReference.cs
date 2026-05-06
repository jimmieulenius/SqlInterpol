using SqlInterpol.Config;

namespace SqlInterpol.References;

public abstract class SqlReference(ISqlFragment parent) : ISqlReference
{
    public ISqlFragment Source { get; } = parent;
    public string? Alias { get; set; }
    public string FallbackAlias { get; init; } = string.Empty;

    public abstract string ToSql(ISqlContext context, SqlRenderMode mode = SqlRenderMode.Default);
}