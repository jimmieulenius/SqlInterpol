using System.Collections.Generic;
using System.Text.RegularExpressions;
using SqlInterpol.Parsing;

namespace SqlInterpol;

/// <summary>
/// Transforms SELECT INTO statements into CREATE TABLE AS SELECT for dialects that do not support it natively.
/// Throws a SqlDialectException for dialects that do not support either structure.
/// </summary>
public class SqlSelectIntoRewriter : ISqlSegmentRewriter
{
    // Opt-out of transpilation if the user disables Meta-SQL!
    public bool IsApplicable(ISqlCompilationState state) => state.Context.Options.MetaSqlTranspilation;

    public IReadOnlyList<SqlSegment> Rewrite(IReadOnlyList<SqlSegment> segments, ISqlContext context)
    {
        int selectIdx = -1;
        int intoIdx = -1;

        // Scan for SELECT ... INTO boundaries
        for (int i = 0; i < segments.Count; i++)
        {
            var seg = segments[i];
            if (seg.HasTag(SqlSegmentTag.SelectKeyword) || seg.HasTag(SqlSegmentTag.SelectDistinctKeyword))
            {
                if (selectIdx == -1) selectIdx = i;
            }
            else if (seg.HasTag(SqlSegmentTag.IntoKeyword))
            {
                intoIdx = i;
                break;
            }
        }

        // If no SELECT INTO found, act as a transparent pass-through
        if (selectIdx == -1 || intoIdx == -1 || intoIdx < selectIdx)
            return segments;

        // ====================================================================
        // DIALECT CAPABILITY GATEKEEPING
        // ====================================================================

        // If the dialect natively supports SELECT INTO, act as a transparent pass-through.
        if (context.Dialect.SupportedFeatures.Contains(SqlFeature.SelectInto))
        {
            return segments;
        }
        
        // If it doesn't support SELECT INTO *and* lacks CREATE TABLE AS SELECT, abort.
        if (!context.Dialect.SupportedFeatures.Contains(SqlFeature.CreateTableAsSelect))
        {
            throw new SqlDialectException("'SELECT INTO' is not supported");
        }

        // ====================================================================
        // CREATE TABLE AS ... FALLBACK TRANSFORMATION
        // ====================================================================

        SqlSegment? targetSegment = null;
        string? targetString = null;
        int targetEndIdx = intoIdx;
        int sliceIdx = 0;

        // Scan forward from INTO to find the actual table target 
        for (int i = intoIdx + 1; i < segments.Count; i++)
        {
            var seg = segments[i];
            if (seg.Type == SqlSegmentType.Literal)
            {
                var text = seg.Value as string;
                if (string.IsNullOrWhiteSpace(text)) continue;

                // Found a non-whitespace literal! Extract the raw target table name right out of it.
                var match = Regex.Match(text!, @"^\s*([a-zA-Z0-9_#\[\]""\`\.]+)");
                if (match.Success)
                {
                    targetString = match.Groups[1].Value;
                    targetEndIdx = i;
                    sliceIdx = match.Length;
                    break;
                }
            }
            else if (seg.Type == SqlSegmentType.Raw || seg.Type == SqlSegmentType.Parameter || seg.Type == SqlSegmentType.Reference)
            {
                targetSegment = seg;
                targetEndIdx = i;
                break;
            }
        }

        if (targetSegment == null && targetString == null) return segments;

        var rewritten = new List<SqlSegment>();

        // 1. Keep everything before SELECT
        for (int i = 0; i < selectIdx; i++) rewritten.Add(segments[i]);

        // 2. CREATE TABLE {Target} AS \n
        rewritten.Add(new SqlSegment(SqlSegmentType.Literal, "CREATE TABLE ", null, null));

        if (targetString != null)
        {
            // Unparameterized: Actively format and quote it!
            string cleanTarget = targetString.Trim('[', ']', '"', '`', '\'');
            rewritten.Add(new SqlSegment(SqlSegmentType.Literal, context.Dialect.QuoteIdentifier(cleanTarget), null, null));
        }
        else if (targetSegment != null)
        {
            // Parameterized: Let the AST natively render it
            rewritten.Add(targetSegment);
        }

        rewritten.Add(new SqlSegment(SqlSegmentType.Literal, " AS\n", null, null));

        // 3. Keep SELECT block up to INTO
        for (int i = selectIdx; i < intoIdx; i++)
        {
            var seg = segments[i];
            // Strip trailing whitespace right before INTO to preserve clean layout
            if (i == intoIdx - 1 && seg.Type == SqlSegmentType.Literal && string.IsNullOrWhiteSpace(seg.Value as string))
                continue; 
            rewritten.Add(seg);
        }

        // 4. Reconstruct the remainder of the literal target block if we dynamically sliced it
        if (targetString != null)
        {
            var seg = segments[targetEndIdx];
            var text = seg.Value as string;
            if (text != null && sliceIdx < text.Length)
            {
                string remainder = text.Substring(sliceIdx);
                string trimmed = remainder.StartsWith("\n") ? remainder.Substring(1).TrimStart(' ', '\t') : remainder.TrimStart();
                
                if (!string.IsNullOrEmpty(trimmed))
                    rewritten.Add(new SqlSegment(SqlSegmentType.Literal, "\n" + trimmed, seg.RenderMode, seg.Tags));
                else
                    rewritten.Add(new SqlSegment(SqlSegmentType.Literal, "\n", seg.RenderMode, seg.Tags));
            }
            else
            {
                rewritten.Add(new SqlSegment(SqlSegmentType.Literal, "\n", null, null));
            }
        }

        // 5. Keep the rest of the query
        for (int i = targetEndIdx + 1; i < segments.Count; i++)
        {
            var seg = segments[i];
            
            // Clean up formatting immediately following the target to prevent double newlines
            if (i == targetEndIdx + 1 && targetSegment != null && seg.Type == SqlSegmentType.Literal && seg.Value is string nStr)
            {
                string trimmed = nStr.StartsWith("\n") ? nStr.Substring(1).TrimStart(' ', '\t') : nStr.TrimStart();
                if (!string.IsNullOrEmpty(trimmed))
                    rewritten.Add(new SqlSegment(SqlSegmentType.Literal, "\n" + trimmed, seg.RenderMode, seg.Tags));
                else
                    rewritten.Add(new SqlSegment(SqlSegmentType.Literal, "\n", seg.RenderMode, seg.Tags));
            }
            else
            {
                // FIX: Removed the buggy extra newline insertion here! Just add the segment.
                rewritten.Add(seg);
            }
        }

        return rewritten;
    }
}