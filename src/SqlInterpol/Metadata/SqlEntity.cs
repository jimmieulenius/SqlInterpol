using System.Linq.Expressions;
using SqlInterpol.Config;
using SqlInterpol.References;

namespace SqlInterpol.Metadata;

public abstract class SqlEntity<T> : ISqlProjection
{
    public string Name { get; }
    public string? Schema { get; }
    public ISqlProjection? Parent { get; }
    public ISqlReference Reference { get; }
    public ISqlDeclaration Declaration { get; }

    protected SqlEntity(string name, string? schema, ISqlProjection? parent = null)
    {
        Name = name;
        Schema = schema;
        Parent = parent;

        Reference = new EntityReference(this);
        Declaration = new SqlDeclaration(Reference);
    }

    // 1. Strongly-Typed Indexer: table[t => t.Name]
    public ISqlReference this[Expression<Func<T, object>> propertySelector]
    {
        get => new SqlColumnReference(Reference, propertySelector);
    }

    // 2. String-Based Indexer: table["Name"]
    public ISqlReference this[string columnName]
    {
        get => new SqlRawColumnReference(Reference, columnName);
    }

    public virtual string ToSql(SqlContext context)
    {
        // If it's a subquery, this is overridden. 
        // If it's a table, it returns [schema].[name]
        return context.Dialect.QuoteTableName(Name, Schema);
    }
}