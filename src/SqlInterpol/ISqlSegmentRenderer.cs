using SqlInterpol.Config;

namespace SqlInterpol;

public interface ISqlSegmentRenderer
{
    string? Render(SqlContext context, SqlSegment segment, int index, IReadOnlyList<SqlSegment> segments);
}