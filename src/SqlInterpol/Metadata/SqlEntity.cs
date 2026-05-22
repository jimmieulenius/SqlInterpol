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
    /// <param name="name">The physical table or view name.</param>
    /// <param name="schema">The schema, or <see langword="null"/> to omit schema qualification.</param>
    /// <param name="alias">
    /// The SQL alias for this entity in query clauses. When provided, it is quoted by default.
    /// When <see langword="null"/>, the CLR type name is used as the fallback alias.
    /// </param>
    protected SqlEntity(string name, string? schema, string? alias = null)
    {
        Name = name;
        Schema = schema;
        
        Reference = new SqlEntityReference(this) 
        { 
            Alias = alias,
            FallbackAlias = typeof(T).Name,
            IsAliasQuoted = !string.IsNullOrWhiteSpace(alias)
        };
        
        Declaration = new SqlDeclaration(this);
    }

    /// <summary>
    /// Renders this entity to a SQL string according to the specified <paramref name="mode"/>.
    /// </summary>
    /// <param name="context">The active context providing dialect and options.</param>
    /// <param name="mode">
    /// Controls the output: <see cref="SqlRenderMode.Declaration"/> emits the full form
    /// (e.g. <c>"Products" AS "p"</c>); <see cref="SqlRenderMode.BaseName"/> emits only the
    /// qualified name; <see cref="SqlRenderMode.AliasOnly"/> emits only the alias;
    /// default emits the reference form used in WHERE and SELECT clauses.
    /// </param>
    /// <returns>The SQL string for this entity in the requested mode.</returns>
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

        string finalAlias = Reference.IsAliasQuoted
            ? context.Dialect.QuoteIdentifier(Reference.Alias)
            : Reference.Alias;
        
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