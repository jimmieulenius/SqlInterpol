using System.Linq.Expressions;
using SqlInterpol.Config;
using SqlInterpol.References;

namespace SqlInterpol.Metadata;

public abstract class SqlEntity<T> : ISqlProjection<T>
{
    public string Name { get; }
    public string? Schema { get; }
    public ISqlProjection? Parent { get; }
    public ISqlReference Reference { get; protected set; }
    public ISqlDeclaration Declaration { get; protected set; }

    protected SqlEntity(string name, string? schema, ISqlProjection? parent = null)
    {
        Name = name;
        Schema = schema;
        Parent = parent;

        // Reference represents the entity in SELECT/JOIN/WHERE (p.[Name])
        Reference = new EntityReference(this);
        
        // Declaration represents the entity in FROM/JOIN ([dbo].[Products] AS p)
        Declaration = new SqlDeclaration(Reference);
    }

    // 1. Strongly-Typed Indexer: table[t => t.Name]
    // Now powered by the Registry!
    public ISqlReference this[Expression<Func<T, object>> propertySelector]
    {
        get
        {
            // Resolve the mapped column name immediately using the cached registry
            string columnName = SqlMetadataRegistry.GetColumnName(propertySelector);
            
            // Pass the resolved string to the reference to avoid re-parsing expressions later
            return new SqlColumnReference(Reference, columnName);
        }
    }

    // 2. String-Based Indexer: table["Name"]
    public ISqlReference this[string columnName]
    {
        get => new SqlRawColumnReference(Reference, columnName);
    }

    public virtual string ToSql(SqlContext context)
    {
        // Dialect handles the specific quoting (e.g., [dbo].[Products] vs "dbo"."Products")
        return context.Dialect.QuoteTableName(Name, Schema);
    }
}