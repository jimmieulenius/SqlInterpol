using SqlInterpol.Configuration;
using SqlInterpol.Schema;
using SqlInterpol.Segments;

namespace SqlInterpol;

/// <summary>
/// Abstract base for concrete SQL entity implementations (<see cref="SqlTable{T}"/>, <see cref="SqlView{T}"/>),
/// providing name, schema, alias management, and mode-aware SQL rendering.
/// </summary>
/// <typeparam name="T">The CLR type mapped to this SQL entity.</typeparam>
public abstract class SqlEntity<T> : SqlEntityBase<T>, ISqlEntity<T>
{
    /// <summary>Gets the physical table or view name.</summary>
    public string Name { get; }
    
    /// <summary>Gets the schema that owns this entity, or <see langword="null"/> for the default schema.</summary>
    public string? Schema { get; }

    /// <summary>
    /// Initializes the entity with a physical name, optional schema, and optional alias.
    /// </summary>
    protected SqlEntity(string name, string? schema = null, string? alias = null)
    {
        Name = name;
        Schema = schema;
        
        Reference = new SqlEntityReference(this) 
        { 
            Alias = alias,
            FallbackAlias = typeof(T).Name,
            IsAliasQuoted = true // Always quote auto/fallback aliases for safety
        };
        
        Declaration = new SqlDeclaration(this);
    }

    /// <summary>
    /// Renders this entity to a SQL string according to the specified <paramref name="mode"/>.
    /// </summary>
    public override string ToSql(ISqlContext context, SqlRenderMode mode = SqlRenderMode.Default)
    {
        return mode switch
        {
            SqlRenderMode.Declaration => RenderDeclaration(context),
            
            SqlRenderMode.BaseName => Role == SqlEntityRole.Cte 
                ? context.Dialect.QuoteIdentifier(Name) // CTEs are strictly schema-less
                : context.Dialect.QuoteEntityName(Name, Schema),
                
            SqlRenderMode.AliasOnly => string.IsNullOrWhiteSpace(Reference.Alias) 
                ? (string.IsNullOrWhiteSpace(Reference.FallbackAlias) ? string.Empty : (Reference.IsAliasQuoted ? context.Dialect.QuoteIdentifier(Reference.FallbackAlias) : Reference.FallbackAlias))
                : Reference.IsAliasQuoted
                    ? context.Dialect.QuoteIdentifier(Reference.Alias)
                    : Reference.Alias,
                    
            _ => RenderReference(context)
        };
    }

    private string RenderDeclaration(ISqlContext context)
    {
        var baseName = Role == SqlEntityRole.Cte 
            ? context.Dialect.QuoteIdentifier(Name)
            : context.Dialect.QuoteEntityName(Name, Schema);
            
        // Incorporate FallbackAlias seamlessly at runtime
        var aliasToUse = Reference.Alias ?? Reference.FallbackAlias;
        
        if (string.IsNullOrWhiteSpace(aliasToUse))
        {
            return baseName;
        }

        string finalAlias = Reference.IsAliasQuoted
            ? context.Dialect.QuoteIdentifier(aliasToUse)
            : aliasToUse;
            
        return context.Dialect.ApplyAlias(baseName, finalAlias);
    }

    private string RenderReference(ISqlContext context)
    {
        var aliasToUse = Reference.Alias ?? Reference.FallbackAlias;
        
        if (!string.IsNullOrWhiteSpace(aliasToUse))
        {
            return Reference.IsAliasQuoted
                ? context.Dialect.QuoteIdentifier(aliasToUse)
                : aliasToUse;
        }

        return Role == SqlEntityRole.Cte 
            ? context.Dialect.QuoteIdentifier(Name)
            : context.Dialect.QuoteEntityName(Name, Schema);
    }
}