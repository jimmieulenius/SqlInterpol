
namespace SqlInterpol;

/// <summary>
/// Base class for SQL fragment collections, rendering items joined by a separator
/// with support for horizontal and <see cref="SqlCollectionLayout.Vertical"/> layouts.
/// </summary>
/// <typeparam name="T">The fragment element type.</typeparam>
/// <param name="items">The fragments to include in the collection.</param>
/// <param name="separator">
/// The separator string between items. When <see langword="null"/>, falls back to
/// <see cref="SqlInterpolOptions.CollectionSeparator"/>.
/// </param>
public class SqlCollectionFragmentBase<T>(IEnumerable<T> items, string? separator = null) 
    : ISqlFragment where T : ISqlFragment
{
    /// <summary>Gets the ordered list of fragment items in this collection.</summary>
    public IReadOnlyList<T> Items { get; } = [.. items];

    /// <inheritdoc />
    public string ToSql(ISqlContext context, SqlRenderMode mode = SqlRenderMode.Default)
    {
        separator ??= context.Options.CollectionSeparator;
        var list = Items.Select(i => i.ToSql(context, mode)).ToList();

        if (list.Count == 0)
        {
            return string.Empty;
        }

        if (context.Options.CollectionLayout == SqlCollectionLayout.Vertical)
        {
            var indent = new string(' ', context.Options.IndentSize);
            return $"{Environment.NewLine}{indent}{string.Join($"{separator.TrimEnd()}{Environment.NewLine}{indent}", list)}";
        }

        return string.Join(separator, list);
    }
}