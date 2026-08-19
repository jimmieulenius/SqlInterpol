using System.Collections.Generic;
using SqlInterpol.Configuration;
using SqlInterpol.Pipeline;
using SqlInterpol.Segments;
using SqlInterpol.Testing.Xunit;
using Xunit;

namespace SqlInterpol.Testing.Specifications;

public abstract partial class PreprocessorExtensibilityTestSuite
{
    /// <summary>
    /// A realistic custom rule that transpiles a proprietary function name safely, 
    /// without breaking string literals, and passes the modified segment back to the core Lexer.
    /// </summary>
    public class SafeKeywordPreprocessorRule : ISqlPreprocessorRule
    {
        public bool Process(ref SqlSegment segment, IReadOnlyList<SqlSegment> segments, int index, SqlPreprocessorState state)
        {
            if (segment.Type == SqlSegmentType.Literal && segment.Value is string text)
            {
                // Use the static helper to safely replace keywords without breaking strings!
                string replaced = SqlLexicalHelper.ReplaceKeyword(text, "MAGIC_FUNC", "REAL_FUNC");
                
                if (replaced != text)
                {
                    // Mutate the reference so the core lexer sees the newly transpiled text
                    segment = new SqlSegment(SqlSegmentType.Literal, replaced, segment.RenderMode, segment.Tags);
                    
                    // Return false so the core lexer STILL runs on this segment to find aliases and parentheses!
                    return false; 
                }
            }
            return false;
        }
    }
}