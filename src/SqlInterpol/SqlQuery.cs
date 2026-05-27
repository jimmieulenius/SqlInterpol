using System.Linq.Expressions;
using SqlInterpol.Parsing;

namespace SqlInterpol;

/// <summary>
/// A compiled SQL query produced by <see cref="SqlBuilder.Query(Action)"/>, holding the
/// captured <see cref="SqlSegment"/> list ready for rendering or further composition.
/// </summary>
/// <param name="builder">The <see cref="SqlBuilder"/> that owns this query.</param>
/// <param name="segments">The ordered list of segments captured from the query body.</param>
public class SqlQuery(SqlBuilder builder, IReadOnlyList<SqlSegment> segments) : ISqlQuery
{
    /// <summary>Gets the <see cref="SqlBuilder"/> that owns this query.</summary>
    public SqlBuilder Builder { get; } = builder;
    /// <summary>Gets the ordered list of <see cref="SqlSegment"/> instances captured from the query body.</summary>
    public IReadOnlyList<SqlSegment> Segments { get; } = segments;

    /// <summary>
    /// Renders this query to a SQL string using the builder's active dialect.
    /// </summary>
    /// <param name="context">The SQL context supplying the dialect and render settings.</param>
    /// <param name="mode">The render mode; only <see cref="SqlRenderMode.Default"/> is meaningful for top-level queries.</param>
    /// <returns>The rendered SQL string.</returns>
    public string ToSql(ISqlContext context, SqlRenderMode mode = SqlRenderMode.Default)
    {
        return Builder.Build(this).Sql;
    }

    /// <summary>
    /// Builds this query into a <see cref="SqlQueryResult"/> containing the rendered SQL and extracted parameters.
    /// </summary>
    /// <returns>The <see cref="SqlQueryResult"/> ready for execution.</returns>
    public SqlQueryResult Build()
    {
        return Builder.Build(this);
    }
}

/// <summary>
/// A typed SQL subquery scoped to a primary entity type <typeparamref name="T"/>.
/// Can be interpolated into an outer query as a subquery, with its alias applied automatically.
/// </summary>
/// <typeparam name="T">The CLR model type of the primary entity this query is scoped to.</typeparam>
public class SqlQuery<T> : SqlEntityBase<T>, ISqlQuery<T>
{
    /// <summary>Gets the <see cref="SqlBuilder"/> that owns this query.</summary>
    public SqlBuilder Builder { get; }
    /// <summary>Gets the ordered list of <see cref="SqlSegment"/> instances captured from the query body.</summary>
    public IReadOnlyList<SqlSegment> Segments => _innerQuery.Segments;

    private readonly ISqlQuery _innerQuery;

    /// <summary>
    /// Initializes a new typed subquery, wiring up the entity reference and alias for use as a composable fragment.
    /// </summary>
    /// <param name="builder">The owning <see cref="SqlBuilder"/>.</param>
    /// <param name="innerQuery">The untyped inner query holding the captured segments.</param>
    /// <param name="alias">
    /// The SQL alias to assign to this subquery. When non-null, the alias will be quoted with the active dialect's
    /// identifier quotes when rendered.
    /// </param>
    public SqlQuery(SqlBuilder builder, ISqlQuery innerQuery, string? alias)
    {
        Builder = builder;
        _innerQuery = innerQuery;

        Reference = new SqlEntityReference(this) 
        { 
            Alias = alias,
            FallbackAlias = typeof(T).Name,
            IsAliasQuoted = !string.IsNullOrWhiteSpace(alias) 
        };
        Declaration = new SqlDeclaration(this);
    }

    /// <summary>
    /// Returns a column projection from this subquery for the property selected by <paramref name="expression"/>.
    /// The projection is bound to this subquery's alias, ensuring correct scoping in the outer query.
    /// </summary>
    /// <param name="expression">A lambda expression selecting the property (e.g. <c>x =&gt; x.Name</c>).</param>
    /// <returns>An <see cref="ISqlProjection"/> scoped to this subquery.</returns>
    public new ISqlProjection this[Expression<Func<T, object?>> expression]
    {
        get
        {
            var member = SqlExpressionHelper.GetProperty(expression);
            return new SqlRawColumnReference(Reference, member.Name);
        }
    }

    /// <summary>
    /// Renders this subquery to SQL, respecting the requested <paramref name="mode"/>.
    /// In <see cref="SqlRenderMode.Declaration"/> the output is wrapped in parentheses with the alias applied;
    /// in <see cref="SqlRenderMode.AliasOnly"/> or <see cref="SqlRenderMode.AsAlias"/> the inner SQL is not built.
    /// </summary>
    /// <param name="context">The SQL context supplying the dialect and identifier-quoting rules.</param>
    /// <param name="mode">Controls which form of the subquery is emitted.</param>
    /// <returns>The rendered SQL fragment for this subquery in the requested mode.</returns>
    public override string ToSql(ISqlContext context, SqlRenderMode mode = SqlRenderMode.Default)
    {
        var aliasToUse = Reference.Alias ?? Reference.FallbackAlias;
        var escapedAlias = Reference.IsAliasQuoted ? context.Dialect.QuoteIdentifier(aliasToUse) : aliasToUse;

        if (mode == SqlRenderMode.AliasOnly) return escapedAlias;
        if (mode == SqlRenderMode.AsAlias) return $"{SqlKeyword.As.Value} {escapedAlias}";

        var innerSql = Builder.Build(_innerQuery).Sql;

        return mode switch
        {
            SqlRenderMode.Declaration => context.Dialect.ApplyAlias($"({innerSql})", escapedAlias),
            SqlRenderMode.BaseName => $"({innerSql})",
            _ => innerSql
        };
    }

    /// <summary>
    /// Builds the inner query into a <see cref="SqlQueryResult"/> containing the rendered SQL and extracted parameters.
    /// </summary>
    /// <returns>The <see cref="SqlQueryResult"/> ready for execution.</returns>
    public SqlQueryResult Build() => Builder.Build(_innerQuery);
}