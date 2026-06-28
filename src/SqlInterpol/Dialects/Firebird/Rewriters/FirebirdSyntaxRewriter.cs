using System.Collections.Generic;
using SqlInterpol.Parsing;

namespace SqlInterpol.Dialects.Firebird; // Hidden from root IntelliSense

public class FirebirdSyntaxRewriter : ISqlSegmentRewriter
{
    public bool IsApplicable(ISqlCompilationState state) => true;

    public IReadOnlyList<SqlSegment> Rewrite(IReadOnlyList<SqlSegment> segments, ISqlContext context)
    {
        var rewritten = new List<SqlSegment>(segments.Count);
        SqlLockMode? deferredLock = null;

        for (int i = 0; i < segments.Count; i++)
        {
            var segment = segments[i];

            if (segment.Type == SqlSegmentType.Raw && segment.Value is SqlLockFragment lockFrag)
            {
                deferredLock = lockFrag.Mode;
                continue;
            }

            if (segment.HasTag(SqlSegmentTag.Paging) && segment.Value is string pagingValue)
            {
                if (i + 3 < segments.Count && segments[i + 1].Type == SqlSegmentType.Parameter && segments[i + 3].Type == SqlSegmentType.Parameter)
                {
                    int index = pagingValue.LastIndexOf(SqlKeyword.Limit, System.StringComparison.OrdinalIgnoreCase);
                    if (index > -1) rewritten.Add(new SqlSegment(SqlSegmentType.Literal, pagingValue[..index]));

                    rewritten.Add(new SqlSegment(SqlSegmentType.Literal, "FIRST "));
                    rewritten.Add(segments[i + 1]); 
                    rewritten.Add(new SqlSegment(SqlSegmentType.Literal, " SKIP "));
                    rewritten.Add(segments[i + 3]); 

                    i += 3;
                    continue;
                }
            }

            rewritten.Add(segment);
        }

        if (deferredLock == SqlLockMode.Update)
            rewritten.Add(new SqlSegment(SqlSegmentType.Literal, "\nWITH LOCK"));

        return rewritten;
    }
}