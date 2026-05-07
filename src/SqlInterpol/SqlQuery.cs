using System.Text;
using SqlInterpol.Config;

namespace SqlInterpol;

public class SqlQuery(SqlBuilder builder, IReadOnlyList<SqlSegment> segments) : ISqlQuery
{
    public SqlBuilder Builder { get; } = builder;
    public IReadOnlyList<SqlSegment> Segments { get; } = segments;

    public string ToSql(ISqlContext context, SqlRenderMode mode = SqlRenderMode.Default)
    {
        var sb = new StringBuilder();
        
        foreach (var segment in Segments)
        {
            if (segment.Type == SqlSegmentType.Literal)
                sb.Append(segment.Value);
            else if (segment.Value is ISqlFragment fragment)
                sb.Append(fragment.ToSql(context, mode));
            else
                sb.Append(segment.Value);
        }
        return sb.ToString();
    }

    public SqlQueryResult Build()
    {
        // PROXY: Let the builder handle the complex rendering and parameter extraction!
        return Builder.Build(this);
    }
}