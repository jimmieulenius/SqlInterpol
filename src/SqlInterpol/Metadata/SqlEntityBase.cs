using System.Linq.Expressions;
using SqlInterpol.Parsing;

namespace SqlInterpol;

/// <summary>
/// Abstract base implementation of <see cref="ISqlEntityBase{T}"/>, providing column access
/// via physical string names and strongly-typed expression selectors.
/// </summary>
/// <typeparam name="T">The CLR type mapped to the SQL entity.</typeparam>
public abstract class SqlEntityBase<T> : ISqlEntityBase<T>, ISqlRoleable
{
    /// <summary>Gets or sets the assigned execution role (e.g. Table or CTE) for this entity.</summary>
    public SqlEntityRole Role { get; set; } = SqlEntityRole.Table;

    /// <summary>Gets or sets the SQL reference fragment for this entity (alias or qualified name used in query clauses).</summary>
    public ISqlReference Reference { get; protected set; } = null!;

    /// <summary>Gets or sets the full declaration fragment for this entity (e.g. <c>"Products" AS "p"</c> in a FROM clause).</summary>
    public ISqlDeclaration Declaration { get; protected set; } = null!;

    // ... (Keep the rest of the class exactly the same)

    public Type ModelType => typeof(T);

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

    [Obsolete("Use the zero-allocation out var syntax and direct POCO property access (e.g., {p.Id}).")]
    public ISqlReference this[Expression<Func<T, object>> propertySelector]
    {
        get
        {
            string propertyName = SqlExpressionHelper.GetPropertyName(propertySelector);
            string columnName = SqlMetadataRegistry.GetColumnName(propertySelector);

            return new SqlColumnReference(
                sourceReference: this.Reference, 
                columnName: columnName, 
                propertyName: propertyName
            );
        }
    }

    public ISqlReference this[string columnName] => new SqlRawColumnReference(Reference, columnName);

    public abstract string ToSql(ISqlContext context, SqlRenderMode mode = SqlRenderMode.Default);
}