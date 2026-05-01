using System.Linq.Expressions;
using SqlInterpol.Config;
using SqlInterpol.References;
using SqlInterpol.Metadata;

namespace SqlInterpol.Metadata;

public abstract class SqlEntity<T> : ISqlEntity<T>
{
    // ISqlEntity implementation
    public string Name { get; set; }
    public string? Schema { get; }
    
    // Internal state
    public ISqlProjection? Parent { get; }
    public ISqlReference Reference { get; protected set; }
    public ISqlDeclaration Declaration { get; protected set; }

    // ISqlProjection implementation
    // For an entity, the PropertyName is the 'Identity' in C#. 
    // We use the Alias if set, otherwise the Table Name.
    public string PropertyName => (Reference as EntityReference)?.Alias ?? Name;

    protected SqlEntity(string name, string? schema, ISqlProjection? parent = null)
    {
        Name = name;
        Schema = schema;
        Parent = parent;

        // 1. Reference acts as the 'Smart Pointer' (Alias ?? Name)
        // We cast 'this' to ISqlEntity to pass it to the reference
        Reference = new EntityReference(this); 
        
        // 2. Declaration represents the source (e.g. [Schema].[Table] AS [Alias])
        Declaration = new SqlDeclaration(Reference);
    }

    // --- Semantic Helpers ---

    public ISqlFragment Entity(string name) => new SqlEntityNameFragment(this, name);

    // public ISqlFragment Column(string name) => new SqlColumnNameFragment(name);
    public ISqlFragment Column(string dbColumnName)
    {
        // We return a deferred fragment
        return new SqlRawFragment(ctx => 
        {
            // 1. Get the current prefix ([dbo].[Products] or [prd])
            var prefix = Reference.ToSql(ctx);
            
            // 2. Quote the manual column name
            var column = ctx.Dialect.QuoteIdentifier(dbColumnName);
            
            // 3. Combine them
            return $"{prefix}.{column}";
        });
    }

    public ISqlFragment Alias(string alias)
    {
        // 1. Immediately update the shared reference.
        // This ensures any columns (rendered before or after) know their prefix.
        Reference.Alias = alias;

        // 2. Return a fragment that tells the Dialect to quote this specific string.
        return new SqlRawFragment(ctx => ctx.Dialect.QuoteIdentifier(alias));
    }

    // --- Indexers ---

    public ISqlReference this[Expression<Func<T, object>> propertySelector]
    {
        get
        {
            string propertyName = SqlMetadataRegistry.GetPropertyName(propertySelector);
            string columnName = SqlMetadataRegistry.GetColumnName(propertySelector);

            return new SqlColumnReference(
                sourceReference: this.Reference, 
                columnName: columnName, 
                propertyName: propertyName
            );
        }
    }

    public ISqlReference this[string columnName] 
        => new SqlRawColumnReference(Reference, columnName);

    // ISqlFragment implementation
    // public virtual string ToSql(SqlContext context) 
    //     => context.Dialect.QuoteTableName(Name, Schema);

    public virtual string ToSql(SqlContext context, SqlRenderMode mode = SqlRenderMode.Default)
    {
        return mode switch
        {
            SqlRenderMode.Declaration => RenderDeclaration(context), // [Table] AS [Alias]
            SqlRenderMode.BaseName    => context.Dialect.QuoteEntityName(Name, Schema), // [Table] (Pure)
            SqlRenderMode.AliasOnly   => RenderReference(context), // [Alias]
            _                         => RenderReference(context)  // [Alias] or [Table]
        };
    }

    private string RenderDeclaration(SqlContext context)
    {
        var tableName = context.Dialect.QuoteEntityName(Name, Schema);
        var alias = Reference.Alias;

        if (string.IsNullOrWhiteSpace(alias))
            return tableName;

        var quotedAlias = context.Dialect.QuoteIdentifier(alias);
        return $"{tableName} AS {quotedAlias}";
    }

    private string RenderReference(SqlContext context)
    {
        // If the user has set an alias, the reference IS the alias.
        // If not, the reference is the full table name.
        return !string.IsNullOrWhiteSpace(Reference.Alias)
            ? context.Dialect.QuoteIdentifier(Reference.Alias)
            : context.Dialect.QuoteEntityName(Name, Schema);
    }
}