using SqlInterpol.Parsing;

namespace SqlInterpol;

/// <summary>
/// Parses raw SQL literals and interpolated values during query construction.
/// </summary>
/// <remarks>
/// The default implementation is <c>SqlInterpolationParser</c>. Supply a custom parser via
/// <see cref="SqlInterpolOptions.Parser"/> to override tokenization or value-to-segment mapping.
/// </remarks>
public interface ISqlInterpolationParser
{
    /// <summary>
    /// Scans a raw SQL literal span and returns a semantic tag if a keyword or special token is detected.
    /// </summary>
    /// <param name="context">The active parser context providing dialect and option state.</param>
    /// <param name="span">The raw SQL text span to inspect.</param>
    /// <returns>A <see cref="SqlSegmentTag"/> string if the literal contains a recognized keyword; otherwise <see langword="null"/>.</returns>
    string? ProcessLiteral(ISqlParserContext context, ReadOnlySpan<char> span);

    /// <summary>
    /// Converts an interpolated value into a typed <see cref="SqlSegment"/>.
    /// </summary>
    /// <remarks>
    /// Handles <see cref="ISqlFragment"/>, <see cref="ISqlReference"/>, collections, enums, and scalar values,
    /// producing the appropriate segment type (Parameter, Fragment, Reference, etc.).
    /// </remarks>
    /// <param name="context">The active parser context.</param>
    /// <param name="value">The interpolated value from the C# string interpolation hole.</param>
    /// <returns>A <see cref="SqlSegment"/> representing the value in the SQL output.</returns>
    SqlSegment ProcessValue(ISqlParserContext context, object? value);

    /// <summary>
    /// Substitutes a keyword token in a SQL string with a dialect-specific replacement.
    /// </summary>
    /// <param name="sql">The SQL string containing the keyword to replace.</param>
    /// <param name="keyword">The keyword token to find.</param>
    /// <param name="replacement">The dialect-specific string to substitute in place of <paramref name="keyword"/>.</param>
    /// <returns>The SQL string with the keyword replaced.</returns>
    string ReplaceKeyword(string sql, string keyword, string replacement);
}