using System.Collections.Generic;
using SqlInterpol.Config;

namespace SqlInterpol;

public class SqlSelectIntoFragment(object targetTable, IReadOnlyList<SqlSegment> sourceSegments, int intoSegmentIndex) : ISqlFragment
{
    public object TargetTable { get; } = targetTable;
    public IReadOnlyList<SqlSegment> SourceSegments { get; } = sourceSegments;
    public int IntoSegmentIndex { get; } = intoSegmentIndex;

    public string ToSql(ISqlContext context, SqlRenderMode renderMode = SqlRenderMode.Default)
    {
        return context.Dialect.RenderFragment(this, context);
    }
}