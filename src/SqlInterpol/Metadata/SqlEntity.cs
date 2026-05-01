using System.Linq.Expressions;
using SqlInterpol.Config;
using SqlInterpol.References;
using SqlInterpol.Metadata;

namespace SqlInterpol.Metadata;

public abstract class SqlEntity<T> : ISqlEntity<T>
{
    public string Name { get; }
    public string? Schema { get; }
    
    public ISqlProjection? Parent { get; }
    public ISqlReference Reference { get; protected set; }
    public ISqlDeclaration Declaration { get; protected set; }
    public string PropertyName => (Reference as SqlEntityReference)?.Alias ?? Name;

    protected SqlEntity(string name, string? schema, ISqlProjection? parent = null)
    {
        Name = name;
        Schema = schema;
        Parent = parent;
        Reference = new SqlEntityReference(this); 
        Declaration = new SqlDeclaration(Reference);
    }

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

    public virtual string ToSql(SqlContext context, SqlRenderMode mode = SqlRenderMode.Default)
    {
        return mode switch
        {
            SqlRenderMode.Declaration => Declaration.ToSql(context),
            SqlRenderMode.BaseName    => context.Dialect.QuoteEntityName(Name, Schema),
            SqlRenderMode.AliasOnly   => RenderReference(context),
            _                         => RenderReference(context)
        };
    }

    private string RenderReference(SqlContext context)
    {
        return !string.IsNullOrWhiteSpace(Reference.Alias)
            ? context.Dialect.QuoteIdentifier(Reference.Alias)
            : context.Dialect.QuoteEntityName(Name, Schema);
    }
}