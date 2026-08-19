using System.Text.RegularExpressions;
using SqlInterpol.Configuration;
using SqlInterpol.Pipeline;
using SqlInterpol.Segments;

namespace SqlInterpol.Testing.Specifications;

public abstract partial class SegmentRewriterTestSuite
{
    public class SoftDeleteRewriter : ISqlSegmentRewriter
    {
        public bool IsApplicable(ISqlPipelineState state) => state.HasTag(SqlSegmentTag.DeleteKeyword);

        public IReadOnlyList<SqlSegment> Rewrite(IReadOnlyList<SqlSegment> segments, ISqlContext context)
        {
            var rewritten = new List<SqlSegment>(segments.Count + 2);
            bool hasInjectedSet = false;

            for (int i = 0; i < segments.Count; i++)
            {
                var seg = segments[i];

                // 1. Convert DELETE to UPDATE
                if (seg.HasTag(SqlSegmentTag.DeleteKeyword))
                {
                    var text = seg.Value?.ToString() ?? "";
                    text = Regex.Replace(text, @"\bDELETE\b", "UPDATE", RegexOptions.IgnoreCase);
                    rewritten.Add(new SqlSegment(SqlSegmentType.Literal, text, seg.RenderMode, SqlSegmentTag.UpdateKeyword));
                }
                // 2. Erase the standalone FROM keyword so it becomes "UPDATE Table" instead of "UPDATE FROM Table"
                else if (seg.HasTag(SqlSegmentTag.FromKeyword))
                {
                    var text = seg.Value?.ToString() ?? "";
                    text = Regex.Replace(text, @"\bFROM\b", "", RegexOptions.IgnoreCase);
                    if (!string.IsNullOrWhiteSpace(text))
                    {
                        // Pass the existing tags array forward
                        rewritten.Add(new SqlSegment(SqlSegmentType.Literal, text, seg.RenderMode, seg.Tags));
                    }
                }
                // 3. Inject the SET clause right before the WHERE clause
                else if (seg.HasTag(SqlSegmentTag.WhereKeyword) && !hasInjectedSet)
                {
                    rewritten.Add(new SqlSegment(SqlSegmentType.Literal, $" SET IsDeleted = 1{Environment.NewLine}"));
                    rewritten.Add(seg);
                    hasInjectedSet = true;
                }
                else
                {
                    rewritten.Add(seg);
                }
            }

            return rewritten;
        }
    }
}