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
            // We render the declaration directly here instead of calling Declaration.ToSql()
            // to prevent the infinite recursion loop.
            SqlRenderMode.Declaration => RenderDeclaration(context),
            
            SqlRenderMode.BaseName => context.Dialect.QuoteEntityName(Name, Schema),
            
            SqlRenderMode.AliasOnly => context.Dialect.QuoteIdentifier(Reference.Alias ?? typeof(T).Name),
            
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
        return $"{baseName} AS {context.Dialect.QuoteIdentifier(Reference.Alias)}";
    }

    private string RenderReference(ISqlContext context)
    {
        return !string.IsNullOrWhiteSpace(Reference.Alias)
            ? context.Dialect.QuoteIdentifier(Reference.Alias)
            : context.Dialect.QuoteEntityName(Name, Schema);
    }
}