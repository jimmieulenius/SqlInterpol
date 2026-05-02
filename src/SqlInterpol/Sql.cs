namespace SqlInterpol;

public static class Sql
{
    public static ISqlFragment Raw(string? value) => 
        new SqlRawFragment(value ?? string.Empty);

    public static ISqlFragment OpenQuote() => 
        new SqlDeferredFragment(ctx => ctx.Dialect.OpenQuote);

    public static ISqlFragment CloseQuote() => 
        new SqlDeferredFragment(ctx => ctx.Dialect.CloseQuote);
}