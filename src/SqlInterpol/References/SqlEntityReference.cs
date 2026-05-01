using SqlInterpol.Config;

namespace SqlInterpol.References;

public class SqlEntityReference(ISqlEntity parent) : ISqlReference
{
    private readonly ISqlEntity _parent = parent;
    
    public string? Alias { get; set; }
    public ISqlEntity Source => _parent;

    public string ToSql(SqlContext context, SqlRenderMode mode = SqlRenderMode.Default)
    {
        // 1. If we have a captured alias (e.g. from "AS [p]"), use it.
        // SQL: [p].[Column]
        if (!string.IsNullOrEmpty(Alias))
        {
            return context.Dialect.QuoteIdentifier(Alias);
        }

        // 2. If NO alias exists, we MUST fall back to the full table name.
        // We delegate this to the Entity's ToSql, which already handles the schema.
        // SQL: [dbo].[Products].[Column]
        return _parent.ToSql(context, mode);
    }
}