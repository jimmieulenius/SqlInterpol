namespace SqlInterpol.Models;

public class SqlSubqueryColumn : SqlReference
{
    private readonly Func<string?> _aliasProvider;

    internal SqlSubqueryColumn(Func<string?> aliasProvider, string columnName)
        : base(columnName, alias: null)
    {
        _aliasProvider = aliasProvider;
    }

    private string ResolvedAlias => _aliasProvider() ??
        throw new InvalidOperationException($"No alias registered for subquery column '{Name}'. Call .As(\"alias\") on the SqlQuery before using .Project<T>() column references.");

    // TODO: Support dialects.
    public override string FullName => $"[{ResolvedAlias}].[{Name}]";

    public override string Reference => FullName;

    public override string ToString(string clause, SqlInterpolOptions options)
    {
        var start = options.IdentifierStart;
        var end = options.IdentifierEnd;

        return $"{start}{ResolvedAlias}{end}.{start}{Name}{end}";
    }

    public override SqlReference As(string alias)
    {
        _alias = alias;

        return this;
    }
}