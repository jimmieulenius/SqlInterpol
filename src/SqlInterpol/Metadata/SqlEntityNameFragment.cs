using SqlInterpol.Config;

namespace SqlInterpol.Metadata;

public record SqlEntityNameFragment(ISqlEntity Entity, string Name) : ISqlFragment
{
    public string ToSql(SqlContext context, SqlRenderMode mode = SqlRenderMode.Default)
        => $"{context.Dialect.OpenQuote}{Name}{context.Dialect.CloseQuote}";
}