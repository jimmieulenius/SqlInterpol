using SqlInterpol.Parsing;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SqlInterpol;

/// <summary>
/// Scans the SQL timeline for complex data modification patterns (like Multi-Table DELETE/UPDATE 
/// with internal JOINs or Inline Views) and packs them into structured AST Fragments.
/// </summary>
public class SqlMultiTableDmlRewriter : ISqlSegmentRewriter
{
    /// <inheritdoc />
    // FIX: Cleanly opt-out of DML transpilation if the user disables Meta-SQL!
    public bool IsApplicable(ISqlCompilationState state) => state.Context.Options.MetaSqlTranspilation;

    /// <inheritdoc />
    public IReadOnlyList<SqlSegment> Rewrite(IReadOnlyList<SqlSegment> segments, ISqlContext context)
    {
        int finalUpdateIdx = -1, finalSetIdx = -1, finalFromIdx = -1, finalWhereIdx = -1;
        int finalDeleteIdx = -1, firstFromIdx = -1, secondFromIdx = -1, finalWhereDeleteIdx = -1;

        for (int i = 0; i < segments.Count; i++)
        {
            var segment = segments[i];
            
            // FIX: Safely trigger finalUpdateIdx if the core rewriter already wrapped the UPDATE AS statement
            if ((segment.HasTag(SqlSegmentTag.UpdateKeyword) || segment.Value is SqlUpdateAsFragment) && finalUpdateIdx == -1) finalUpdateIdx = i;
            else if ((segment.HasTag(SqlSegmentTag.SetKeyword) || segment.Value is SqlSetFragment) && finalSetIdx == -1) finalSetIdx = i;
            else if ((segment.HasTag(SqlSegmentTag.DeleteKeyword) || segment.Value is SqlDeleteAsFragment) && finalDeleteIdx == -1) finalDeleteIdx = i;
            
            if (segment.HasTag(SqlSegmentTag.FromKeyword))
            {
                if (finalFromIdx == -1) finalFromIdx = i;
                if (firstFromIdx == -1) firstFromIdx = i;
                else if (secondFromIdx == -1) secondFromIdx = i;
            }
            else if (segment.HasTag(SqlSegmentTag.WhereKeyword))
            {
                if (finalWhereIdx == -1) finalWhereIdx = i;
                if (finalWhereDeleteIdx == -1) finalWhereDeleteIdx = i;
            }
        }

        // CASE A: Standard Multi-Table Join Updates
        if (finalUpdateIdx >= 0 && finalSetIdx > finalUpdateIdx && finalFromIdx > finalSetIdx)
        {
            ISqlFragment target;
            
            // FIX: Safely extract the target if it was wrapped by the Core Syntax Rewriter
            if (segments[finalUpdateIdx].Value is SqlUpdateAsFragment upAs)
            {
                target = upAs.Target;
            }
            else
            {
                target = new SqlSegmentCollectionFragment([.. segments.Skip(finalUpdateIdx + 1).Take(finalSetIdx - finalUpdateIdx - 1)]);
            }

            var endOfSet = finalFromIdx > 0 ? finalFromIdx : (finalWhereIdx > 0 ? finalWhereIdx : segments.Count);
            
            List<SqlSegment> setClauseSegments = segments[finalSetIdx].Value is SqlSetFragment 
                ? [segments[finalSetIdx]]
                : segments.Skip(finalSetIdx + 1).Take(endOfSet - finalSetIdx - 1).Select(s => 
                    (s.Type == SqlSegmentType.Projection || s.Type == SqlSegmentType.Raw) && s.Value is ISqlProjection p ? new SqlSegment(s.Type, p, SqlRenderMode.BaseName, s.Tags) : s
                  ).ToList();
            
            var setClause = new SqlSegmentCollectionFragment(setClauseSegments);
            int endOfFrom = finalWhereIdx > 0 ? finalWhereIdx : segments.Count;
            var fromClause = new SqlSegmentCollectionFragment([.. segments.Skip(finalFromIdx + 1).Take(endOfFrom - finalFromIdx - 1)]);
            SqlSegmentCollectionFragment? whereClause = finalWhereIdx > 0 ? new SqlSegmentCollectionFragment([.. segments.Skip(finalWhereIdx + 1)]) : null;

            return [new SqlSegment(SqlSegmentType.Raw, new SqlMultiTableUpdateFragment(target, setClause, fromClause, whereClause))];
        }

        // CASE B: Inline View Subquery Updates
        if (finalUpdateIdx >= 0 && finalSetIdx > finalUpdateIdx && finalFromIdx == -1)
        {
            int targetIdx = -1;
            ISqlQueryFragment? subquery = null;
            string alias = "stats";

            // FIX: Peek inside the wrapped SqlUpdateAsFragment to successfully capture inline views
            if (segments[finalUpdateIdx].Value is SqlUpdateAsFragment upAsFragment && upAsFragment.Target is ISqlQueryFragment q)
            {
                targetIdx = finalUpdateIdx;
                subquery = q;
                if (!string.IsNullOrEmpty(upAsFragment.Target.Reference?.Alias))
                {
                    alias = context.Dialect.UnquoteIdentifier(upAsFragment.Target.Reference.Alias);
                }
            }
            else
            {
                for (int k = finalUpdateIdx + 1; k < finalSetIdx; k++) 
                {
                    if (segments[k].Value is ISqlQueryFragment sq) 
                    { 
                        targetIdx = k; 
                        subquery = sq;
                        break; 
                    }
                }
            }

            if (targetIdx > -1 && subquery != null)
            {
                if (segments[targetIdx].Value is not SqlUpdateAsFragment)
                {
                    for (int k = targetIdx + 1; k < finalSetIdx; k++)
                    {
                        var valStr = segments[k].Value?.ToString()?.Trim();
                        if (string.IsNullOrEmpty(valStr) || string.Equals(valStr, "AS", StringComparison.OrdinalIgnoreCase)) continue;
                        var cleanIdentifier = context.Dialect.UnquoteIdentifier(valStr);
                        
                        if (IsWord(cleanIdentifier)) alias = cleanIdentifier;
                    }
                }

                var endOfSet = finalWhereIdx > 0 ? finalWhereIdx : segments.Count;
                
                List<SqlSegment> setClauseSegments = segments[finalSetIdx].Value is SqlSetFragment 
                    ? [segments[finalSetIdx]] 
                    : segments.Skip(finalSetIdx + 1).Take(endOfSet - finalSetIdx - 1).Select(s => 
                        (s.Type == SqlSegmentType.Projection || s.Type == SqlSegmentType.Raw) && s.Value is ISqlProjection p ? new SqlSegment(s.Type, p, SqlRenderMode.BaseName, s.Tags) : s
                      ).ToList();
                
                var setClause = new SqlSegmentCollectionFragment(setClauseSegments);
                SqlSegmentCollectionFragment? whereClause = finalWhereIdx > 0 ? new SqlSegmentCollectionFragment(segments.Skip(finalWhereIdx + 1).ToList()) : null;

                subquery.ExcludeParentheses = true;

                if (!context.Dialect.SupportedFeatures.Contains(SqlFeature.UpdatableInlineViews)) 
                {
                    return [new SqlSegment(SqlSegmentType.Raw, new SqlUpdateCteFragment(alias, subquery, setClause, whereClause))];
                }
                return [new SqlSegment(SqlSegmentType.Raw, new SqlUpdateSubqueryFragment(subquery, alias, setClause, whereClause))];
            }
        }

        // CASE C: Complex Multi-Table Delete joins
        if (finalDeleteIdx > -1 && firstFromIdx > -1)
        {
            bool isMultiTableDelete = false;
            int targetStartIndex = -1, targetEndIndex = -1, fromClauseStartIndex = -1;
            ISqlFragment? explicitTarget = null;

            // FIX: Safely unwrap target if core rewriter already applied DeleteAs isolation
            if (segments[finalDeleteIdx].Value is SqlDeleteAsFragment delAs)
            {
                isMultiTableDelete = true;
                explicitTarget = delAs.Target;
                fromClauseStartIndex = firstFromIdx + 1;
            }
            else
            {
                var segmentsBetween = segments.Skip(finalDeleteIdx + 1).Take(firstFromIdx - finalDeleteIdx - 1).ToList();
                bool hasExplicitTarget = segmentsBetween.Any(s => s.Type != SqlSegmentType.Literal || !string.IsNullOrWhiteSpace(s.Value?.ToString()));

                if (hasExplicitTarget)
                {
                    isMultiTableDelete = true;
                    targetStartIndex = finalDeleteIdx + 1;
                    targetEndIndex = firstFromIdx;
                    fromClauseStartIndex = firstFromIdx + 1;
                }
                else if (secondFromIdx > -1)
                {
                    isMultiTableDelete = true;
                    targetStartIndex = firstFromIdx + 1;
                    targetEndIndex = secondFromIdx;
                    fromClauseStartIndex = secondFromIdx + 1;
                }
            }

            if (isMultiTableDelete)
            {
                ISqlFragment targetFrag;
                if (explicitTarget != null) 
                {
                    targetFrag = explicitTarget;
                }
                else
                {
                    var targetSegments = segments.Skip(targetStartIndex).Take(targetEndIndex - targetStartIndex).ToList();
                    targetFrag = new SqlSegmentCollectionFragment(targetSegments);
                }

                var fromSegments = finalWhereDeleteIdx > -1 ? segments.Skip(fromClauseStartIndex).Take(finalWhereDeleteIdx - fromClauseStartIndex).ToList() : segments.Skip(fromClauseStartIndex).ToList();
                var whereSegments = finalWhereDeleteIdx > -1 ? segments.Skip(finalWhereDeleteIdx + 1).ToList() : null;

                return [new SqlSegment(SqlSegmentType.Raw, new SqlMultiTableDeleteFragment(targetFrag, new SqlSegmentCollectionFragment(fromSegments), whereSegments != null ? new SqlSegmentCollectionFragment(whereSegments) : null))];
            }
        }

        return segments;
    }

    private static bool IsWord(string? text)
    {
        if (string.IsNullOrEmpty(text)) return false;
        for (int i = 0; i < text.Length; i++)
        {
            char c = text[i];
            if (!char.IsLetterOrDigit(c) && c != '_') return false;
        }
        return true;
    }
}