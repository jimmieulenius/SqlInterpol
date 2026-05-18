using SqlInterpol.Config;

namespace SqlInterpol.References;

public class SqlEntityReference(ISqlFragment parent) : ISqlReference
{
    private readonly ISqlFragment _parent = parent;
    
    public string? Alias { get; set; }
    public bool IsAliasQuoted { get; set; }
    public required string FallbackAlias { get; set; }
    public ISqlFragment Source => _parent;

    public string ToSql(ISqlContext context, SqlRenderMode mode = SqlRenderMode.Default)
    {
        if (!string.IsNullOrEmpty(Alias))
        {
            // return context.Dialect.QuoteIdentifier(Alias);
            return IsAliasQuoted ? context.Dialect.QuoteIdentifier(Alias) : Alias;
        }

        return _parent.ToSql(context, mode);
    }
}