// using SqlInterpol.Config;

// namespace SqlInterpol.Metadata;

// public record SqlColumnNameFragment(string Name) : ISqlFragment
// {
//     public string ToSql(SqlContext context, SqlRenderMode mode = SqlRenderMode.Default)
//     {
//         return $"{context.Dialect.OpenQuote}{Name}{context.Dialect.CloseQuote}";
//     }
// }