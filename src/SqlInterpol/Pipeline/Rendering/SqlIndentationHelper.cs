namespace SqlInterpol.Pipeline;

/// <summary>
/// Provides indentation extraction utilities shared across the preprocessing
/// and rendering stages of the SQL pipeline.
/// </summary>
internal static class SqlIndentationHelper
{
    /// <summary>
    /// Extracts the leading whitespace (spaces and tabs) from the last line of
    /// <paramref name="prevText"/>, for use when indenting an inlined fragment.
    /// Returns <see cref="string.Empty"/> if no newline or no leading whitespace is found.
    /// </summary>
    /// <param name="prevText">The text of the segment immediately preceding the fragment.</param>
    /// <returns>The indent string (e.g., <c>"    "</c>) or <see cref="string.Empty"/>.</returns>
    public static string ExtractTrailingLineIndent(string? prevText)
    {
        if (string.IsNullOrEmpty(prevText)) return string.Empty;

        int lastNewline = prevText.LastIndexOf('\n');
        if (lastNewline < 0) return string.Empty;

        int start = lastNewline + 1;
        int end = start;
        while (end < prevText.Length && (prevText[end] == ' ' || prevText[end] == '\t'))
            end++;

        return end > start ? prevText[start..end] : string.Empty;
    }

    /// <summary>
    /// Applies <paramref name="indent"/> to every newline within <paramref name="rendered"/>.
    /// Returns the original string unchanged when <paramref name="indent"/> is empty.
    /// </summary>
    /// <param name="rendered">The fragment text to indent.</param>
    /// <param name="indent">The indent prefix to insert after each newline character.</param>
    /// <returns>The indented string.</returns>
    public static string ApplyIndent(string rendered, string indent)
        => indent.Length == 0 ? rendered : rendered.Replace("\n", $"\n{indent}");
}
