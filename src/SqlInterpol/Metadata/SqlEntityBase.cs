using System.Linq.Expressions;
using SqlInterpol.Config;
using SqlInterpol.References;

namespace SqlInterpol.Metadata;

public abstract class SqlEntityBase<T> : ISqlEntityBase<T>
{
    public ISqlReference Reference { get; protected set; } = null!;
    public ISqlDeclaration Declaration { get; protected set; } = null!;

    public ISqlFragment Entity(string name) => new SqlEntityNameFragment(this, name);

    public ISqlFragment Column(string dbColumnName)
    {
        return new SqlDeferredFragment(ctx => 
        {
            var prefix = Reference.ToSql(ctx);
            var column = ctx.Dialect.QuoteIdentifier(dbColumnName);

            return $"{prefix}.{column}";
        });
    }

    public ISqlFragment Alias(string alias)
    {
        Reference.Alias = alias;

        return new SqlDeferredFragment(ctx => ctx.Dialect.QuoteIdentifier(alias));
    }

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

    public abstract string ToSql(ISqlContext context, SqlRenderMode mode = SqlRenderMode.Default);
}