using SqlInterpol.Config;

namespace SqlInterpol;

public class SqlCollectionFragmentBase<T>(IEnumerable<T> items, string? separator = null) 
    : ISqlFragment where T : ISqlFragment
{
    public IReadOnlyList<T> Items { get; } = [.. items];

    public string ToSql(ISqlContext context, SqlRenderMode mode = SqlRenderMode.Default)
    {
        if (Items.Count == 0)
        {
            return string.Empty;
        }

        string actualSeparator = separator ?? context.Options.CollectionSeparator;

        var renderedFragments = Items.Select(f => f.ToSql(context, mode));

        return string.Join(actualSeparator, renderedFragments);
    }
}