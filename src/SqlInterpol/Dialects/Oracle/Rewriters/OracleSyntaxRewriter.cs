using SqlInterpol.Parsing;
using SqlInterpol.Rewriters;

namespace SqlInterpol.Dialects.Oracle;

/// <summary>
/// A structural rewriter that transforms EXCEPT to MINUS, handles recursive CTE syntax, 
/// strips invalid AS aliases recursively, transpiles LIMIT/OFFSET paging, and safely repositions deferred locks for Oracle.
/// </summary>
public class OracleSyntaxRewriter : SqlSyntaxRewriterBase
{
    private SqlLockMode? _deferredLock;

    // Activates the alias dropping routine in the base class!
    protected override bool DropTableAliasAsKeyword => true;

    protected override SqlSegment ProcessRecursiveSegments(SqlSegment segment, ISqlContext context)
    {
        if (segment.Value is SqlUpdateSubqueryFragment updateSubquery && updateSubquery.Subquery is SqlNestedQueryFragment subNested)
        {
            var rewrittenInner = Rewrite(subNested.Segments.ToList(), context).ToList();
            var newNested = new SqlNestedQueryFragment(rewrittenInner, subNested.Reference) { ExcludeParentheses = subNested.ExcludeParentheses };
            return new SqlSegment(segment.Type, new SqlUpdateSubqueryFragment(newNested, updateSubquery.Alias, updateSubquery.SetClause, updateSubquery.WhereClause), segment.RenderMode, segment.Tags);
        }
        if (segment.Value is SqlUpdateCteFragment updateCte && updateCte.Subquery is SqlNestedQueryFragment cteNested)
        {
            var rewrittenInner = Rewrite(cteNested.Segments.ToList(), context).ToList();
            var newNested = new SqlNestedQueryFragment(rewrittenInner, cteNested.Reference) { ExcludeParentheses = cteNested.ExcludeParentheses };
            return new SqlSegment(segment.Type, new SqlUpdateCteFragment(updateCte.Alias, newNested, updateCte.SetClause, updateCte.WhereClause), segment.RenderMode, segment.Tags);
        }
        if (segment.Value is SqlNestedQueryFragment nestedQuery)
        {
            var rewrittenInner = Rewrite(nestedQuery.Segments.ToList(), context).ToList();
            var newNestedFragment = new SqlNestedQueryFragment(rewrittenInner, nestedQuery.Reference) { ExcludeParentheses = nestedQuery.ExcludeParentheses };
            return new SqlSegment(segment.Type, newNestedFragment, segment.RenderMode, segment.Tags);
        }
        return segment;
    }

    protected override string ProcessLiteral(string literal)
    {
        var newValue = literal;
        if (newValue.Contains("WITH RECURSIVE", StringComparison.OrdinalIgnoreCase)) newValue = SqlSegmentPreprocessor.SafeReplaceKeyword(newValue, "WITH RECURSIVE", "WITH");
        if (newValue.Contains(SqlKeyword.Except.Value, StringComparison.OrdinalIgnoreCase)) newValue = SqlSegmentPreprocessor.SafeReplaceKeyword(newValue, SqlKeyword.Except.Value, "MINUS");
        return newValue;
    }

    protected override bool TryRewriteLock(SqlLockFragment lockFrag, IReadOnlyList<SqlSegment> segments, List<SqlSegment> rewritten, ref int i)
    {
        if (lockFrag.Mode == SqlLockMode.Update)
        {
            _deferredLock = lockFrag.Mode;
            return true;
        }
        return false;
    }

    protected override bool TryRewritePaging(SqlSegment segment, IReadOnlyList<SqlSegment> segments, List<SqlSegment> rewritten, ref int i)
    {
        // FIX: Use the new hybrid node extractor!
        if (!segment.HasTag(SqlSegmentTag.Paging) || !SqlRewriterHelpers.TryExtractPagingNodes(segments, i, out var limitNode, out var offsetNode, out int nextIndex, out string trailingText)) 
            return false;

        if (segment.Value is string textPaging)
        {
            int index = textPaging.LastIndexOf(SqlKeyword.Limit.Value, StringComparison.OrdinalIgnoreCase);
            if (index > -1) rewritten.Add(new SqlSegment(SqlSegmentType.Literal, textPaging[..index]));
        }

        rewritten.Add(new SqlSegment(SqlSegmentType.Literal, "OFFSET "));
        rewritten.Add(offsetNode); 
        rewritten.Add(new SqlSegment(SqlSegmentType.Literal, " ROWS FETCH NEXT "));
        rewritten.Add(limitNode); 
        rewritten.Add(new SqlSegment(SqlSegmentType.Literal, " ROWS ONLY"));

        // Append any unrecognized characters (like semicolons or comments) that were trailing the integers
        if (!string.IsNullOrEmpty(trailingText))
        {
            rewritten.Add(new SqlSegment(SqlSegmentType.Literal, " " + trailingText.TrimStart()));
        }

        i = nextIndex;
        return true;
    }

    protected override void ApplyDeferredTransforms(List<SqlSegment> rewritten, ISqlContext context)
    {
        if (_deferredLock == SqlLockMode.Update) rewritten.Add(new SqlSegment(SqlSegmentType.Literal, "\nFOR UPDATE"));
        base.ApplyDeferredTransforms(rewritten, context);
    }
}