using SqlInterpol.Config;

namespace SqlInterpol.Metadata;

public record SqlEntityNameFragment(ISqlEntity Entity, string Name) : ISqlFragment
{
    public string ToSql(SqlContext context, SqlRenderMode mode = SqlRenderMode.Default)
    {
        // Anchor the entity in the parser state so the next 'AS' alias 
        // is captured and assigned to this entity.
        context.ParseState.PendingAliasCapture = Entity;
        
        return $"{context.Dialect.OpenQuote}{Name}{context.Dialect.CloseQuote}";
    }
}