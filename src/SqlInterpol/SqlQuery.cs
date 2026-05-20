using System.Linq.Expressions;
using SqlInterpol.Parsing;

namespace SqlInterpol;

public class SqlQuery(SqlBuilder builder, IReadOnlyList<SqlSegment> segments) : ISqlQuery
{
    public SqlBuilder Builder { get; } = builder;
    public IReadOnlyList<SqlSegment> Segments { get; } = segments;

    public string ToSql(ISqlContext context, SqlRenderMode mode = SqlRenderMode.Default)
    {
        return Builder.Build(this).Sql;
    }

    public SqlQueryResult Build()
    {
        return Builder.Build(this);
    }
}

public class SqlQuery<T> : SqlEntityBase<T>, ISqlQuery<T>
{
    public SqlBuilder Builder { get; }
    public IReadOnlyList<SqlSegment> Segments => _innerQuery.Segments;

    private readonly ISqlQuery _innerQuery;

    public SqlQuery(SqlBuilder builder, ISqlQuery innerQuery, string? alias)
    {
        Builder = builder;
        _innerQuery = innerQuery;

        // WYSIWYG FIX: Ensure programmatic subquery aliases are safely quoted by default!
        Reference = new SqlEntityReference(this) 
        { 
            Alias = alias,
            FallbackAlias = typeof(T).Name,
            IsAliasQuoted = !string.IsNullOrWhiteSpace(alias) 
        };
        Declaration = new SqlDeclaration(this);
    }

    public new ISqlProjection this[Expression<Func<T, object?>> expression]
    {
        get
        {
            var member = SqlExpressionHelper.GetMember(expression);
            // We create a projection that knows its source is THIS subquery
            return new SqlRawColumnReference(Reference, member.Name);
        }
    }

    public override string ToSql(ISqlContext context, SqlRenderMode mode = SqlRenderMode.Default)
    {
        var aliasToUse = Reference.Alias ?? Reference.FallbackAlias;
        
        // Respect the quoting intent!
        var escapedAlias = Reference.IsAliasQuoted ? context.Dialect.QuoteIdentifier(aliasToUse) : aliasToUse;

        // Short-circuit: these modes don't need the inner SQL built
        if (mode == SqlRenderMode.AliasOnly) return escapedAlias;
        if (mode == SqlRenderMode.AsAlias) return $"{SqlKeyword.As.Value} {escapedAlias}";

        // Build the inner query.
        var innerSql = Builder.Build(_innerQuery).Sql;

        return mode switch
        {
            // Use the dialect to cleanly apply the alias
            SqlRenderMode.Declaration => context.Dialect.ApplyAlias($"({innerSql})", escapedAlias),
            SqlRenderMode.BaseName => $"({innerSql})",
            _ => innerSql
        };
    }

    public SqlQueryResult Build() => Builder.Build(_innerQuery);
}