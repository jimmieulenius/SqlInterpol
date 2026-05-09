using SqlInterpol.Config;

namespace SqlInterpol;

public sealed class SqlRawCollectionFragment(List<string> raw) : ISqlFragment
{
    public string ToSql(ISqlContext context, SqlRenderMode mode = SqlRenderMode.Default)
    {
        if (raw.Count == 0)
        {
            return string.Empty;
        }

        return string.Join(context.Options.CollectionSeparator, raw);
    }
}