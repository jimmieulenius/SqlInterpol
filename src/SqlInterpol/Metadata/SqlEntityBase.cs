using System.Linq.Expressions;
using SqlInterpol.Parsing;

namespace SqlInterpol;

/// <summary>
/// Abstract base implementation of <see cref="ISqlEntityBase{T}"/>, providing column access
/// via physical string names and strongly-typed expression selectors.
/// </summary>
/// <typeparam name="T">The CLR type mapped to the SQL entity.</typeparam>
public abstract class SqlEntityBase<T> : ISqlEntityBase<T>
{
    /// <summary>Gets or sets the SQL reference fragment for this entity (alias or qualified name used in query clauses).</summary>
    public ISqlReference Reference { get; protected set; } = null!;

    /// <summary>Gets or sets the full declaration fragment for this entity (e.g. <c>"Products" AS "p"</c> in a FROM clause).</summary>
    public ISqlDeclaration Declaration { get; protected set; } = null!;

    /// <summary>
    /// Gets a fragment that renders a quoted sub-entity identifier scoped to this entity.
    /// </summary>
    /// <param name="name">The identifier to render.</param>
    /// <returns>An <see cref="ISqlFragment"/> that renders the quoted name.</returns>
    public ISqlFragment Entity(string name) => new SqlEntityNameFragment(this, name);

    /// <summary>
    /// Gets a deferred fragment that renders the fully qualified column name (e.g. <c>"p"."Name"</c>).
    /// </summary>
    /// <param name="dbColumnName">The physical column name.</param>
    /// <returns>An <see cref="ISqlFragment"/> that resolves the qualified column at render time.</returns>
    public ISqlFragment Column(string dbColumnName)
    {
        return new SqlDeferredFragment(ctx => 
        {
            var prefix = Reference.ToSql(ctx);
            var column = ctx.Dialect.QuoteIdentifier(dbColumnName);

            return $"{prefix}.{column}";
        });
    }

    // TODO (v2.0): Remove when deleting old lambda syntax
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

    /// <summary>Gets a raw column reference by physical column name.</summary>
    /// <param name="columnName">The physical column name.</param>
    /// <returns>An <see cref="ISqlReference"/> for the specified column on this entity.</returns>
    public ISqlReference this[string columnName] 
        => new SqlRawColumnReference(Reference, columnName);

    /// <inheritdoc />
    public abstract string ToSql(ISqlContext context, SqlRenderMode mode = SqlRenderMode.Default);
}