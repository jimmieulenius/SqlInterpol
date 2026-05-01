using SqlInterpol.Config;

namespace SqlInterpol.References;

public class SqlEntityReference(ISqlFragment parent) : ISqlReference
{
    private readonly ISqlFragment _parent = parent;
    
    public string? Alias { get; set; }
    public ISqlFragment Source => _parent;

    public string ToSql(SqlContext context, SqlRenderMode mode = SqlRenderMode.Default)
    {
        if (!string.IsNullOrEmpty(Alias))
        {
            return context.Dialect.QuoteIdentifier(Alias);
        }

        return _parent.ToSql(context, mode);
    }
}