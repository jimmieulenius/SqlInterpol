namespace SqlInterpol;

public static class Sql
{
    public static ISqlFragment OpenQuote { get; } = 
        new SqlDeferredFragment(ctx => ctx.Dialect.OpenQuote);

    public static ISqlFragment CloseQuote { get; } = 
        new SqlDeferredFragment(ctx => ctx.Dialect.CloseQuote);

    public static ISqlFragment GroupBy(params ISqlFragment[] fragments)
    {
        return new SqlCollectionFragment(fragments);
    }
    
    public static ISqlFragment GroupBy(IEnumerable<ISqlFragment> fragments)
    {
        return new SqlCollectionFragment(fragments);
    }

    public static ISqlOrderFragment OrderBy(
        ISqlReference reference, 
        SqlOrderDirection direction = SqlOrderDirection.Asc) 
    {
        return new SqlOrderFragment(reference, direction);
    }

    public static ISqlOrderFragment OrderBy(IEnumerable<ISqlOrderFragment> fragments)
    {
        return new SqlOrderCollectionFragment(fragments);
    }
    
    public static ISqlOrderFragment OrderBy(params ISqlOrderFragment[] fragments)
    {
        return new SqlOrderCollectionFragment(fragments);
    }

    public static ISqlFragment Quote(string value) => 
        new SqlDeferredFragment(ctx => ctx.Dialect.QuoteIdentifier(value));

    public static ISqlFragment Raw(string? value) => 
        new SqlRawFragment(value ?? string.Empty);
}