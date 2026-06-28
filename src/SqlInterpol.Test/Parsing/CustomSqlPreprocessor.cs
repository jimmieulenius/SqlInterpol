using System.Collections;
using SqlInterpol.Parsing;

namespace SqlInterpol.Test.Parsing;

/// <summary>
/// A custom preprocessor that acts as a middleware pipeline step.
/// It intercepts "CUSTOM_IN" keywords, maps the adjacent collection, 
/// and then delegates the rest of the work to the standard preprocessor.
/// </summary>
public class CustomSqlPreprocessor : ISqlSegmentPreprocessor
{
    public IReadOnlyList<SqlSegment> Process(IReadOnlyList<SqlSegment> segments, ISqlContext context)
    {
        var modifiedSegments = new List<SqlSegment>(segments.Count);
        bool nextIsCollection = false;

        for (int i = 0; i < segments.Count; i++)
        {
            var segment = segments[i];

            if (segment.Type == SqlSegmentType.Literal && segment.Value is string text)
            {
                // 1. Sniff for our custom keyword
                if (text.TrimEnd().EndsWith("CUSTOM_IN", StringComparison.OrdinalIgnoreCase))
                {
                    nextIsCollection = true;
                }
                else if (text.Trim().Length > 0)
                {
                    nextIsCollection = false;
                }
                
                modifiedSegments.Add(segment);
            }
            else if (nextIsCollection && segment.Type == SqlSegmentType.Unresolved && segment.Value is IEnumerable enumerable and not string)
            {
                // 2. Intercept the unresolved collection and upgrade it to our custom Fragment immediately!
                modifiedSegments.Add(new SqlSegment(SqlSegmentType.Raw, new SqlInListFragment(enumerable)));
                nextIsCollection = false;
            }
            else
            {
                // 3. Pass through everything else untouched
                modifiedSegments.Add(segment);
            }
        }

        // 4. Hand the modified list to the standard Preprocessor so it can 
        // safely handle normal parameters, DTO mapping, and standard keyword tracking!
        return SqlSegmentPreprocessor.Instance.Process(modifiedSegments, context);
    }
}