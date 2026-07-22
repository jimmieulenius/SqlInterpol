using System.Text;
using SqlInterpol.Configuration;

namespace SqlInterpol.Segments;

/// <summary>
/// Renders a list of segments into a SQL string by delegating each segment to the context renderer.
/// </summary>
/// <param name="segments">The ordered list of segments to render.</param>
public class SqlSegmentCollectionFragment(IReadOnlyList<SqlSegment> segments) : ISqlFragment
{
    /// <summary>Gets the ordered list of segments in this collection.</summary>
    public IReadOnlyList<SqlSegment> Segments { get; } = segments;

    /// <inheritdoc />
    public string ToSql(ISqlContext context, SqlRenderMode renderMode = SqlRenderMode.Default)
    {
        var sb = new StringBuilder();
        for (int i = 0; i < Segments.Count; i++)
        {
            sb.Append(context.Renderer.Render(context, Segments[i], i, Segments));
        }
        return sb.ToString();
    }
}