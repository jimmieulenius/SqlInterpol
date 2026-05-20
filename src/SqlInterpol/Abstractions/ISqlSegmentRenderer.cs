
namespace SqlInterpol;

public interface ISqlSegmentRenderer
{
    string? Render(ISqlContext context, SqlSegment segment, int index, IReadOnlyList<SqlSegment> segments);
}