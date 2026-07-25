using SqlInterpol.Configuration;
using SqlInterpol.Segments;

namespace SqlInterpol.Schema;

/// <summary>
/// Represents a concrete database view bound to a CLR model type.
/// </summary>
/// <typeparam name="T">The CLR model type representing the view schema.</typeparam>
public class SqlView<T> : SqlEntityBase<T>
{
    /// <summary>Gets the physical name of the view.</summary>
    public string Name { get; }
    
    /// <summary>Gets the database schema the view belongs to, if any.</summary>
    public string? Schema { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="SqlView{T}"/> class.
    /// </summary>
    /// <param name="name">The physical view name.</param>
    /// <param name="schema">The database schema.</param>
    /// <param name="alias">The explicit alias for this view within the query scope.</param>
    public SqlView(string name, string? schema, string? alias)
    {
        Name = name;
        Schema = schema;
        Reference = new SqlEntityReference(this) 
        { 
            Alias = alias, 
            FallbackAlias = typeof(T).Name, // FIX: Use the C# Type Name!
            IsAliasQuoted = !string.IsNullOrWhiteSpace(alias) 
        };
        Declaration = new SqlDeclaration(this);
    }

    /// <inheritdoc />
    public override string ToSql(ISqlContext context, SqlRenderMode mode = SqlRenderMode.Default)
    {
        var dialect = context.Dialect;
        
        if (mode == SqlRenderMode.AliasOnly)
        {
            var aliasToUse = Reference.Alias ?? Reference.FallbackAlias;
            return Reference.IsAliasQuoted && aliasToUse != null 
                ? dialect.QuoteIdentifier(aliasToUse) 
                : aliasToUse ?? Name;
        }

        // FIX: Respect CTE roles by rendering strictly schema-less
        var quotedName = Role == SqlEntityRole.Cte 
            ? dialect.QuoteIdentifier(Name) 
            : dialect.QuoteEntityName(Name, Schema);
        
        if (mode == SqlRenderMode.Declaration)
        {
            var aliasToUse = Reference.Alias ?? Reference.FallbackAlias;
            return string.IsNullOrEmpty(aliasToUse) 
                ? quotedName 
                : dialect.ApplyAlias(quotedName, Reference.IsAliasQuoted ? dialect.QuoteIdentifier(aliasToUse) : aliasToUse);
        }

        return quotedName;
    }
}