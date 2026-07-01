using System.Linq.Expressions;
using SqlInterpol.Parsing;

namespace SqlInterpol;

/// <summary>
/// A compiled SQL query produced by <see cref="SqlBuilder.Query(Action)"/>, holding the
/// captured <see cref="SqlSegment"/> list ready for rendering or further composition.
/// </summary>
public class SqlQuery(SqlBuilder builder, IReadOnlyList<SqlSegment> segments) : ISqlQuery
{
    public SqlBuilder Builder { get; } = builder;
    public IReadOnlyList<SqlSegment> Segments { get; } = segments;
    
    /// <inheritdoc />
    public bool ExcludeParentheses { get; set; }

    /// <inheritdoc />
    public string ToSql(ISqlContext context, SqlRenderMode mode = SqlRenderMode.Default)
    {
        var sql = Builder.Build(this).Sql;
        
        // Deferred rendering naturally respects the AST state!
        if (ExcludeParentheses) return sql;
        
        return $"({sql})";
    }

    /// <inheritdoc />
    public SqlQueryResult Build() => Builder.Build(this);
}

/// <summary>
/// A typed SQL subquery scope bound to a primary entity model type <typeparamref name="T"/>.
/// </summary>
public class SqlQuery<T> : SqlEntityBase<T>, ISqlQuery<T>
{
    public SqlBuilder Builder { get; }
    
    /// <inheritdoc />
    public IReadOnlyList<SqlSegment> Segments => _innerQuery.Segments;
    
    /// <inheritdoc />
    public bool ExcludeParentheses 
    { 
        get => _innerQuery.ExcludeParentheses; 
        set => _innerQuery.ExcludeParentheses = value; 
    }

    private readonly ISqlQuery _innerQuery;

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

    [Obsolete("Use the zero-allocation out var syntax and direct POCO property access.")]
    public new ISqlProjection this[Expression<Func<T, object?>> expression]
    {
        get
        {
            var member = SqlExpressionHelper.GetProperty(expression);
            return new SqlRawColumnReference(Reference, member.Name);
        }
    }

    /// <inheritdoc />
    public override string ToSql(ISqlContext context, SqlRenderMode mode = SqlRenderMode.Default)
    {
        var aliasToUse = Reference.Alias ?? Reference.FallbackAlias;
        var escapedAlias = Reference.IsAliasQuoted ? context.Dialect.QuoteIdentifier(aliasToUse) : aliasToUse;

        if (mode == SqlRenderMode.AliasOnly) return escapedAlias;
        if (mode == SqlRenderMode.AsAlias) return context.Dialect.ApplyAlias("", escapedAlias).Trim();

        var innerSql = Builder.Build(_innerQuery).Sql;

        if (ExcludeParentheses) return innerSql;

        return mode switch
        {
            SqlRenderMode.Declaration => context.Dialect.ApplyAlias($"({innerSql})", escapedAlias),
            SqlRenderMode.BaseName => $"({innerSql})",
            _ => $"({innerSql})"
        };
    }

    /// <inheritdoc />
    public SqlQueryResult Build() => Builder.Build(_innerQuery);
}