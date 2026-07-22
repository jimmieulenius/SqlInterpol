using SqlInterpol.Configuration;
using SqlInterpol.Segments;

namespace SqlInterpol.Pipeline;

/// <summary>
/// Renders a single <see cref="SqlSegment"/> to its SQL string representation.
/// </summary>
/// <remarks>
/// The default implementation is <c>SqlSegmentRenderer</c>. Supply a custom
/// <see cref="ISqlSegmentRenderer"/> via <see cref="SqlInterpolOptions.Renderer"/> to
/// override how individual segment types are emitted.
/// </remarks>
public interface ISqlSegmentRenderer
{
    /// <summary>
    /// Renders the given segment to a SQL string fragment.
    /// </summary>
    /// <param name="context">The active <see cref="ISqlContext"/> providing dialect, options, and parameter state.</param>
    /// <param name="segment">The segment to render.</param>
    /// <param name="index">The zero-based position of <paramref name="segment"/> within <paramref name="segments"/>.</param>
    /// <param name="segments">The full ordered list of segments, for context-sensitive rendering.</param>
    /// <returns>The SQL string for this segment, or <see langword="null"/> to emit nothing.</returns>
    string? Render(ISqlContext context, SqlSegment segment, int index, IReadOnlyList<SqlSegment> segments);
}