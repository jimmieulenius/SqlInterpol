using SqlInterpol.Config;

namespace SqlInterpol.References;

public class EntityReference(ISqlProjection source) : SqlReference(source)
{
    public override string ToSql(SqlContext context)
    {
        // 1. Prioritize the AliasName
        if (!string.IsNullOrEmpty(Alias))
        {
            return context.Dialect.QuoteIdentifier(Alias);
        }

        // 2. Otherwise, fall back to the Source's own rendering 
        // (which for an Entity is the Quoted Table Name)
        return Source.ToSql(context);
    }
}