using System;
using System.Collections.Generic;
using SqlInterpol.Parsing;

namespace SqlInterpol.Dialects.Firebird;

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

            if (segment.Type == SqlSegmentType.Raw && segment.Value is SqlLockFragment lockFrag && lockFrag.Mode == SqlLockMode.Update)
            {
                deferredLock = lockFrag.Mode;
                continue;
            }

            if (segment.HasTag(SqlSegmentTag.Paging) && SqlRewriterHelpers.TryExtractPagingParameters(segments, i, out var limitParam, out var offsetParam, out int nextIndex))
            {
                if (segment.Value is string pagingValue)
                {
                    int index = pagingValue.LastIndexOf(SqlKeyword.Limit.Value, StringComparison.OrdinalIgnoreCase);
                    if (index > -1) rewritten.Add(new SqlSegment(SqlSegmentType.Literal, pagingValue[..index]));
                }

                rewritten.Add(new SqlSegment(SqlSegmentType.Literal, "FIRST "));
                rewritten.Add(limitParam); 
                rewritten.Add(new SqlSegment(SqlSegmentType.Literal, " SKIP "));
                rewritten.Add(offsetParam); 

                i = nextIndex;
                continue;
            }

            rewritten.Add(segment);
        }

        if (deferredLock == SqlLockMode.Update)
            rewritten.Add(new SqlSegment(SqlSegmentType.Literal, "\nWITH LOCK"));

        return rewritten;
    }
}