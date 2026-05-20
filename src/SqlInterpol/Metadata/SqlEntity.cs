
namespace SqlInterpol;

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
            FallbackAlias = typeof(T).Name,
            IsAliasQuoted = !string.IsNullOrWhiteSpace(alias)
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
                : Reference.IsAliasQuoted
                    ? context.Dialect.QuoteIdentifier(Reference.Alias)
                    : Reference.Alias,
            // SqlRenderMode.AliasOnly => string.IsNullOrWhiteSpace(Reference.Alias) 
            //     ? string.Empty 
            //     : context.Dialect.QuoteIdentifier(Reference.Alias),
            
            _ => RenderReference(context)
        };
    }

    private string RenderDeclaration(ISqlContext context)
    {
        var baseName = context.Dialect.QuoteEntityName(Name, Schema);
        
        if (string.IsNullOrWhiteSpace(Reference.Alias))
        {
            return baseName;
        }

        // Apply safely quoted (or explicitly unquoted) alias based on user intent
        string finalAlias = Reference.IsAliasQuoted
            ? context.Dialect.QuoteIdentifier(Reference.Alias)
            :Reference.Alias;
        
        return context.Dialect.ApplyAlias(baseName, finalAlias);
    }

    private string RenderReference(ISqlContext context)
    {
        if (!string.IsNullOrWhiteSpace(Reference.Alias))
        {
            return Reference.IsAliasQuoted
                ? context.Dialect.QuoteIdentifier(Reference.Alias)
                : Reference.Alias;
        }

        return context.Dialect.QuoteEntityName(Name, Schema);
    }
}