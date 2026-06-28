using System.Text.RegularExpressions;
using SqlInterpol.Parsing;

namespace SqlInterpol.Dialects.Oracle;

/// <summary>
/// A structural rewriter that transforms EXCEPT to MINUS, handles recursive CTE syntax, 
/// strips invalid AS aliases recursively, and safely repositions deferred locks for Oracle.
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

            if (segment.Type == SqlSegmentType.Raw && segment.Value is SqlLockFragment lockFrag)
            {
                deferredLock = lockFrag.Mode;
                continue;
            }

            // ====================================================================
            // RECURSIVE DRILLING PASS
            // ====================================================================
            
            // FIX: Safely unwrap and drill into subqueries hidden by higher-level DML layout AST nodes!
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
            // TEXT & TAG CLEANUP PASS
            // ====================================================================
            
            if (segment.HasTag(SqlSegmentTag.TableAliasAsKeyword))
            {
                droppedAs = true;
                continue; // Drops table-bound "AS" keywords at any nesting layer
            }

            if (segment.Type == SqlSegmentType.Literal && segment.Value is string literalValue)
            {
                var newValue = literalValue;

                // Collapse the ghost space left behind by the dropped AS keyword
                if (droppedAs)
                {
                    if (newValue.StartsWith(" ")) newValue = newValue[1..];
                    droppedAs = false;
                }

                if (newValue.Contains("WITH RECURSIVE", StringComparison.OrdinalIgnoreCase))
                    newValue = newValue.Replace("WITH RECURSIVE", "WITH", StringComparison.OrdinalIgnoreCase);

                if (newValue.Contains("EXCEPT", StringComparison.OrdinalIgnoreCase))
                    newValue = Regex.Replace(newValue, @"(?i)\bEXCEPT\b", "MINUS");

                if (!ReferenceEquals(newValue, literalValue))
                    segment = new SqlSegment(SqlSegmentType.Literal, newValue, segment.RenderMode, segment.Tags);
            }
            else if (droppedAs)
            {
                // Reset the flag if the next item isn't a text literal
                droppedAs = false;
            }

            rewritten.Add(segment);
        }

        if (deferredLock == SqlLockMode.Update || deferredLock == SqlLockMode.Share)
        {
            rewritten.Add(new SqlSegment(SqlSegmentType.Literal, "\nFOR UPDATE"));
        }

        return rewritten;
    }
}