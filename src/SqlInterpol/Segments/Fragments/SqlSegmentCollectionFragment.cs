using System.Collections.Generic;
using System.Text;
using SqlInterpol.Configuration;
using SqlInterpol.Pipeline;

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
        var renderer = context.Options?.Renderer ?? SqlSegmentRenderer.Instance;

        for (int i = 0; i < Segments.Count; i++)
        {
            sb.Append(renderer.Render(context, Segments[i], i, Segments));
        }
        
        return sb.ToString();
    }
}