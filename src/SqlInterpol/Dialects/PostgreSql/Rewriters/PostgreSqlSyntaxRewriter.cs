using SqlInterpol.Parsing;

namespace SqlInterpol.Dialects.PostgreSql;

/// <summary>
/// A structural rewriter that safely repositions deferred locks for PostgreSQL.
/// </summary>
public class PostgreSqlSyntaxRewriter : ISqlSegmentRewriter
{
    /// <inheritdoc />
    public bool IsApplicable(ISqlCompilationState state) => true;

    /// <inheritdoc />
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
            rewritten.Add(segment);
        }

        if (deferredLock == SqlLockMode.Update)
            rewritten.Add(new SqlSegment(SqlSegmentType.Literal, "\nFOR UPDATE"));
        else if (deferredLock == SqlLockMode.Share)
            rewritten.Add(new SqlSegment(SqlSegmentType.Literal, "\nFOR SHARE"));

        return rewritten;
    }
}