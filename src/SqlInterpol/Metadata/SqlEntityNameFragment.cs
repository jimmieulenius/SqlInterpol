
namespace SqlInterpol;

public record SqlEntityNameFragment(ISqlEntityBase Entity, string Name) : ISqlFragment
{
    public string ToSql(ISqlContext context, SqlRenderMode mode = SqlRenderMode.Default)
        => $"{context.Dialect.OpenQuote}{Name}{context.Dialect.CloseQuote}";
}