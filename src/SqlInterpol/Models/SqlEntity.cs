using System.Linq.Expressions;
using SqlInterpol.Abstractions;

namespace SqlInterpol.Models;

public abstract class SqlEntity<T> : ISqlProjection
{
    public string Name { get; }
    public string? Schema { get; }
    public ISqlProjection? Parent { get; }
    public ISqlReference Reference { get; }
    public ISqlDeclaration Declaration { get; }

    protected SqlEntity(string name, string? schema = null, ISqlProjection? parent = null)
    {
        Name = name;
        Schema = schema;
        Parent = parent;

        // Concrete Reference/Declaration instances
        // TableReference/TableDeclaration can actually be renamed to 
        // EntityReference/EntityDeclaration to be truly DRY.
        Reference = new EntityReference(this);
        Declaration = new SqlDeclaration(Reference);
    }

    // This defines what the physical source looks like: [dbo].[Name]
    public virtual string ToSql(SqlContext context)
    {
        return context.Dialect.QuoteTableName(Name, Schema);
    }

    public ISqlReference GetColumnReference(string propertyName, SqlInterpolOptions options)
    {
        throw new NotImplementedException();
    }

    // Property indexer for columns: table[x => x.Name]
    public ISqlReference this[Expression<Func<T, object>> propertySelector]
    {
        get 
        {
            var memberName = GetMemberName(propertySelector);
            
            return new ColumnReference(Reference, memberName);
        }
    }
}