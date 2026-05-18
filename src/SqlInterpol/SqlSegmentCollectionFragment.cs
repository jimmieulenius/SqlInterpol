using System.Text;
using SqlInterpol.Config;
using SqlInterpol.Rendering;

namespace SqlInterpol;

public class SqlSegmentCollectionFragment(IReadOnlyList<SqlSegment> segments) : ISqlFragment
{
    public IReadOnlyList<SqlSegment> Segments { get; } = segments;

    public string ToSql(ISqlContext context, SqlRenderMode renderMode = SqlRenderMode.Default)
    {
        var sb = new StringBuilder();
        
        for (int i = 0; i < Segments.Count; i++)
        {
            sb.Append(SqlSegmentRenderer.Instance.Render(context, Segments[i], i, Segments));
        }

        return sb.ToString();
    }
}