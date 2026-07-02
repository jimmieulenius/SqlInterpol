using System.Text.RegularExpressions;
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
        
        // Tracks if we just dropped an 'AS' keyword so we can clean up the ghost space left behind
        bool droppedAs = false;

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

            // ====================================================================
            // RECURSIVE DRILLING PASS
            // ====================================================================
            
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
                var newNestedFragment = new SqlNestedQueryFragment(rewrittenInner, nestedQuery.Reference)
                {
                    ExcludeParentheses = nestedQuery.ExcludeParentheses
                };
                segment = new SqlSegment(segment.Type, newNestedFragment, segment.RenderMode, segment.Tags);
            }

            // ====================================================================
            // PAGING TRANSFORMATION PASS
            // ====================================================================
            
            if (segment.HasTag(SqlSegmentTag.Paging) && segment.Value is string textPaging)
            {
                int p1Idx = i + 1;
                while (p1Idx < segments.Count && segments[p1Idx].Type == SqlSegmentType.Literal && string.IsNullOrWhiteSpace(segments[p1Idx].Value as string)) p1Idx++;
                
                int p2Idx = p1Idx + 1;
                while (p2Idx < segments.Count && segments[p2Idx].Type == SqlSegmentType.Literal && string.IsNullOrWhiteSpace(segments[p2Idx].Value as string)) p2Idx++;
                
                int p3Idx = p2Idx + 1;
                while (p3Idx < segments.Count && segments[p3Idx].Type == SqlSegmentType.Literal && string.IsNullOrWhiteSpace(segments[p3Idx].Value as string)) p3Idx++;

                if (p3Idx < segments.Count && segments[p1Idx].Type == SqlSegmentType.Parameter && segments[p3Idx].Type == SqlSegmentType.Parameter)
                {
                    int index = textPaging.LastIndexOf("LIMIT", StringComparison.OrdinalIgnoreCase);
                    if (index > -1) rewritten.Add(new SqlSegment(SqlSegmentType.Literal, textPaging[..index]));

                    rewritten.Add(new SqlSegment(SqlSegmentType.Literal, "OFFSET "));
                    rewritten.Add(segments[p3Idx]); 
                    
                    rewritten.Add(new SqlSegment(SqlSegmentType.Literal, " ROWS FETCH NEXT "));
                    rewritten.Add(segments[p1Idx]); 
                    
                    rewritten.Add(new SqlSegment(SqlSegmentType.Literal, " ROWS ONLY"));

                    i = p3Idx;
                    continue;
                }
            }

            // ====================================================================
            // TEXT & TAG CLEANUP PASS
            // ====================================================================
            
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

                // FIX: Use the state-aware replacement to protect strings and comments!
                if (newValue.Contains("WITH RECURSIVE", StringComparison.OrdinalIgnoreCase))
                    newValue = SqlSegmentPreprocessor.SafeReplaceKeyword(newValue, "WITH RECURSIVE", "WITH");

                if (newValue.Contains("EXCEPT", StringComparison.OrdinalIgnoreCase))
                    newValue = SqlSegmentPreprocessor.SafeReplaceKeyword(newValue, "EXCEPT", "MINUS");

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