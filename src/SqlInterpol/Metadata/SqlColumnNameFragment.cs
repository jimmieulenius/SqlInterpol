using SqlInterpol.Config;

namespace SqlInterpol.Metadata;

public record SqlColumnNameFragment(string Name) : ISqlFragment
{
    public string ToSql(SqlContext context)
    {
        return $"{context.Dialect.OpenQuote}{Name}{context.Dialect.CloseQuote}";
    }
}