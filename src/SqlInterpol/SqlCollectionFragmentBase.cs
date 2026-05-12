using SqlInterpol.Config;

namespace SqlInterpol;

public class SqlCollectionFragmentBase<T>(IEnumerable<T> items, string? separator = null) 
    : ISqlFragment where T : ISqlFragment
{
    public IReadOnlyList<T> Items { get; } = [.. items];

    public string ToSql(ISqlContext context, SqlRenderMode mode = SqlRenderMode.Default)
    {
        separator ??= context.Options.CollectionSeparator;
        var list = Items.Select(i => i.ToSql(context, mode)).ToList();

        if (list.Count == 0)
        {
            return string.Empty;
        }

        // Use the global option to decide layout
        if (context.Options.CollectionLayout == SqlCollectionLayout.Vertical)
        {
            var indent = new string(' ', context.Options.IndentSize);
            // Result:
            //     item1,
            //     item2,
            //     item3
            return $"{Environment.NewLine}{indent}{string.Join($"{separator.TrimEnd()}{Environment.NewLine}{indent}", list)}";
        }

        return string.Join(separator, list);
    }
}