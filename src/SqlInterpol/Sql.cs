namespace SqlInterpol;

public static class Sql
{
    // Cached as auto-properties to avoid allocating a new fragment every time!
    public static ISqlFragment OpenQuote { get; } = 
        new SqlDeferredFragment(ctx => ctx.Dialect.OpenQuote);

    public static ISqlFragment CloseQuote { get; } = 
        new SqlDeferredFragment(ctx => ctx.Dialect.CloseQuote);

    public static ISqlFragment Raw(string? value) => 
        new SqlRawFragment(value ?? string.Empty);

    public static ISqlOrderFragment OrderBy(
        ISqlReference reference, 
        SqlOrderDirection direction = SqlOrderDirection.Asc) 
    {
        return new SqlOrderFragment(reference, direction);
    }

    public static ISqlFragment Quote(string value) => 
        new SqlDeferredFragment(ctx => ctx.Dialect.QuoteIdentifier(value));
}