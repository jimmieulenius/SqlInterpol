using System;
using System.Collections.Generic;
using System.Linq;
using SqlInterpol.Parsing;

namespace SqlInterpol.Dialects.Oracle;

/// <summary>
/// A structural rewriter that transforms EXCEPT to MINUS, handles recursive CTE syntax, 
/// strips invalid AS aliases recursively, transpiles LIMIT/OFFSET paging, and safely repositions deferred locks for Oracle.
/// </summary>
public class OracleSyntaxRewriter : ISqlSegmentRewriter
{
    /// <inheritdoc />
    public bool IsApplicable(ISqlCompilationState state) => true;

    /// <inheritdoc />
    public IReadOnlyList<SqlSegment> Rewrite(IReadOnlyList<SqlSegment> segments, ISqlContext context)
    {
        var rewritten = new List<SqlSegment>(segments.Count);
        SqlLockMode? deferredLock = null;
        bool droppedAs = false;

        for (int i = 0; i < segments.Count; i++)
        {
            var segment = segments[i];

            if (segment.Type == SqlSegmentType.Raw && segment.Value is SqlLockFragment lockFrag && lockFrag.Mode == SqlLockMode.Update)
            {
                deferredLock = lockFrag.Mode;
                continue;
            }

            if (segment.Value is SqlUpdateSubqueryFragment updateSubquery && updateSubquery.Subquery is SqlNestedQueryFragment subNested)
            {
                var rewrittenInner = Rewrite(subNested.Segments.ToList(), context).ToList();
                var newNested = new SqlNestedQueryFragment(rewrittenInner, subNested.Reference) { ExcludeParentheses = subNested.ExcludeParentheses };
                segment = new SqlSegment(segment.Type, new SqlUpdateSubqueryFragment(newNested, updateSubquery.Alias, updateSubquery.SetClause, updateSubquery.WhereClause), segment.RenderMode, segment.Tags);
            }
            else if (segment.Value is SqlUpdateCteFragment updateCte && updateCte.Subquery is SqlNestedQueryFragment cteNested)
            {
                var rewrittenInner = Rewrite(cteNested.Segments.ToList(), context).ToList();
                var newNested = new SqlNestedQueryFragment(rewrittenInner, cteNested.Reference) { ExcludeParentheses = cteNested.ExcludeParentheses };
                segment = new SqlSegment(segment.Type, new SqlUpdateCteFragment(updateCte.Alias, newNested, updateCte.SetClause, updateCte.WhereClause), segment.RenderMode, segment.Tags);
            }
            else if (segment.Value is SqlNestedQueryFragment nestedQuery)
            {
                var rewrittenInner = Rewrite(nestedQuery.Segments.ToList(), context).ToList();
                var newNestedFragment = new SqlNestedQueryFragment(rewrittenInner, nestedQuery.Reference) { ExcludeParentheses = nestedQuery.ExcludeParentheses };
                segment = new SqlSegment(segment.Type, newNestedFragment, segment.RenderMode, segment.Tags);
            }

            if (segment.HasTag(SqlSegmentTag.Paging) && SqlRewriterHelpers.TryExtractPagingParameters(segments, i, out var limitParam, out var offsetParam, out int nextIndex))
            {
                if (segment.Value is string textPaging)
                {
                    int index = textPaging.LastIndexOf(SqlKeyword.Limit.Value, StringComparison.OrdinalIgnoreCase);
                    if (index > -1) rewritten.Add(new SqlSegment(SqlSegmentType.Literal, textPaging[..index]));
                }

                rewritten.Add(new SqlSegment(SqlSegmentType.Literal, "OFFSET "));
                rewritten.Add(offsetParam); 
                
                rewritten.Add(new SqlSegment(SqlSegmentType.Literal, " ROWS FETCH NEXT "));
                rewritten.Add(limitParam); 
                
                rewritten.Add(new SqlSegment(SqlSegmentType.Literal, " ROWS ONLY"));

                i = nextIndex;
                continue;
            }

            if (segment.HasTag(SqlSegmentTag.TableAliasAsKeyword))
            {
                droppedAs = true;
                continue; 
            }

            if (segment.Type == SqlSegmentType.Literal && segment.Value is string literalValue)
            {
                var newValue = literalValue;

                if (droppedAs)
                {
                    if (newValue.StartsWith(" ")) newValue = newValue[1..];
                    droppedAs = false;
                }

                if (newValue.Contains("WITH RECURSIVE", StringComparison.OrdinalIgnoreCase))
                    newValue = SqlSegmentPreprocessor.SafeReplaceKeyword(newValue, "WITH RECURSIVE", "WITH");

                if (newValue.Contains(SqlKeyword.Except.Value, StringComparison.OrdinalIgnoreCase))
                    newValue = SqlSegmentPreprocessor.SafeReplaceKeyword(newValue, SqlKeyword.Except.Value, "MINUS");

                if (!ReferenceEquals(newValue, literalValue))
                    segment = new SqlSegment(SqlSegmentType.Literal, newValue, segment.RenderMode, segment.Tags);
            }
            else if (droppedAs)
            {
                droppedAs = false;
            }

            rewritten.Add(segment);
        }

        if (deferredLock == SqlLockMode.Update)
        {
            rewritten.Add(new SqlSegment(SqlSegmentType.Literal, "\nFOR UPDATE"));
        }

        return rewritten;
    }
}