using SqlInterpol.Configuration;
using SqlInterpol.Segments;

namespace SqlInterpol.Schema;

/// <summary>
/// Represents a concrete database table bound to a CLR model type.
/// </summary>
/// <typeparam name="T">The CLR model type representing the table schema.</typeparam>
public class SqlTable<T> : SqlEntityBase<T>
{
    /// <summary>Gets the physical name of the table.</summary>
    public string Name { get; }
    
    /// <summary>Gets the database schema the table belongs to, if any.</summary>
    public string? Schema { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="SqlTable{T}"/> class.
    /// </summary>
    /// <param name="name">The physical table name.</param>
    /// <param name="schema">The database schema.</param>
    /// <param name="alias">The explicit alias for this table within the query scope.</param>
    public SqlTable(string name, string? schema, string? alias)
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