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

            // FIX: Only intercept and defer FOR UPDATE locks. Let FOR SHARE locks pass through 
            // so the global capabilities pipeline can catch and reject them natively!
            if (segment.Type == SqlSegmentType.Raw && segment.Value is SqlLockFragment lockFrag && lockFrag.Mode == SqlLockMode.Update)
            {
                deferredLock = lockFrag.Mode;
                continue;
            }

            // Restored Paging syntax rewriter and safely scan past structural whitespace!
            if (segment.HasTag(SqlSegmentTag.Paging) && segment.Value is string pagingValue)
            {
                int p1Idx = i + 1;
                while (p1Idx < segments.Count && segments[p1Idx].Type == SqlSegmentType.Literal && string.IsNullOrWhiteSpace(segments[p1Idx].Value as string)) p1Idx++;
                
                int p2Idx = p1Idx + 1;
                while (p2Idx < segments.Count && segments[p2Idx].Type == SqlSegmentType.Literal && string.IsNullOrWhiteSpace(segments[p2Idx].Value as string)) p2Idx++;
                
                int p3Idx = p2Idx + 1;
                while (p3Idx < segments.Count && segments[p3Idx].Type == SqlSegmentType.Literal && string.IsNullOrWhiteSpace(segments[p3Idx].Value as string)) p3Idx++;

                if (p3Idx < segments.Count && segments[p1Idx].Type == SqlSegmentType.Parameter && segments[p3Idx].Type == SqlSegmentType.Parameter)
                {
                    int index = pagingValue.LastIndexOf(SqlKeyword.Limit, StringComparison.OrdinalIgnoreCase);
                    if (index > -1) rewritten.Add(new SqlSegment(SqlSegmentType.Literal, pagingValue[..index]));

                    rewritten.Add(new SqlSegment(SqlSegmentType.Literal, "FIRST "));
                    rewritten.Add(segments[p1Idx]); 
                    rewritten.Add(new SqlSegment(SqlSegmentType.Literal, " SKIP "));
                    rewritten.Add(segments[p3Idx]); 

                    i = p3Idx;
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