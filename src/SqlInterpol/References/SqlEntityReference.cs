using SqlInterpol.Config;

namespace SqlInterpol.References;

public class EntityReference(ISqlEntity parent) : ISqlReference
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

// public class EntityReference(ISqlEntity entity) : ISqlReference
// {
//     public ISqlProjection Source => Entity;
//     public ISqlEntity Entity => entity;
//     public string? Alias { get; set; }

//     public string ToSql(SqlContext context)
//     {
//         var open = context.Dialect.OpenQuote;
//         var close = context.Dialect.CloseQuote;

//         // For a subquery, Name is empty, so Alias is mandatory.
//         string identifier = Alias ?? Entity.Name;

//         if (string.IsNullOrEmpty(identifier))
//         {
//             // This would only happen if someone forgot to alias a subquery.
//             // Most DBs (SQL Server/Postgres) will throw an error anyway.
//             return string.Empty; 
//         }
        
//         return $"{open}{identifier}{close}";
//     }
// }