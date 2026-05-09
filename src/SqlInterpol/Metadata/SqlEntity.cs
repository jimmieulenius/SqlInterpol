using SqlInterpol.Config;
using SqlInterpol.References;

namespace SqlInterpol.Metadata;

public abstract class SqlEntity<T> : SqlEntityBase<T>, ISqlEntity<T>
{
    public string Name { get; }
    public string? Schema { get; }

    protected SqlEntity(string name, string? schema, string? alias = null)
    {
        Name = name;
        Schema = schema;
        
        // Initialize the reference with the provided alias and the Type name as fallback
        Reference = new SqlEntityReference(this) 
        { 
            Alias = alias,
            FallbackAlias = typeof(T).Name 
        };
        
        // Declaration points to this entity to render the 'Table AS Alias' string
        Declaration = new SqlDeclaration(this);
    }

    public override string ToSql(ISqlContext context, SqlRenderMode mode = SqlRenderMode.Default)
    {
        return mode switch
        {
            SqlRenderMode.Declaration => RenderDeclaration(context),
            
            SqlRenderMode.BaseName => context.Dialect.QuoteEntityName(Name, Schema),
            
            SqlRenderMode.AliasOnly => string.IsNullOrWhiteSpace(Reference.Alias) 
                ? string.Empty 
                : context.Dialect.QuoteIdentifier(Reference.Alias),
            
            _ => RenderReference(context)
        };
    }

    private string RenderDeclaration(ISqlContext context)
    {
        var baseName = context.Dialect.QuoteEntityName(Name, Schema);
        
        // If no alias is defined, just return the physical name (e.g., [dbo].[Products])
        if (string.IsNullOrWhiteSpace(Reference.Alias))
        {
            return baseName;
        }

        // Return the full declaration (e.g., [dbo].[Products] AS [p])
        return context.Dialect.ApplyAlias(baseName, Reference.Alias);
    }

    private string RenderReference(ISqlContext context)
    {
        return !string.IsNullOrWhiteSpace(Reference.Alias)
            ? context.Dialect.QuoteIdentifier(Reference.Alias)
            : context.Dialect.QuoteEntityName(Name, Schema);
    }
}