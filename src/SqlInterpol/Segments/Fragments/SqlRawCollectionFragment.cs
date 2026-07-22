using SqlInterpol.Configuration;

namespace SqlInterpol.Segments;

/// <summary>
/// A collection fragment for raw SQL string items, rendering them joined by a separator.
/// </summary>
/// <param name="items">The raw SQL string items to include.</param>
public sealed class SqlRawCollectionFragment(List<string> items) : ISqlFragment
{
    /// <inheritdoc />
    public string ToSql(ISqlContext context, SqlRenderMode mode = SqlRenderMode.Default)
    {
        var separator = context.Options.CollectionSeparator;
        var list = items.ToList();
        
        if (list.Count == 0) return string.Empty;

        if (context.Options.CollectionLayout == SqlCollectionLayout.Vertical)
        {
            separator = separator.TrimEnd();
            var indent = new string(' ', context.Options.IndentSize);
            return string.Join($"{separator}{Environment.NewLine}{indent}", list);
        }
        return string.Join(separator, list);
    }
}