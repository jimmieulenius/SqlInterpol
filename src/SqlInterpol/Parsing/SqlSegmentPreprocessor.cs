using System.Collections.Generic;

namespace SqlInterpol.Parsing;

/// <summary>
/// Tier 1 Runtime Preprocessor. 
/// Extremely fast, zero-allocation pipeline focused ONLY on structural blocks.
/// </summary>
public partial class SqlSegmentPreprocessor : ISqlSegmentPreprocessor
{
    public static readonly SqlSegmentPreprocessor Instance = new();

    /// <inheritdoc />
    public IReadOnlyList<SqlSegment> Process(IReadOnlyList<SqlSegment> segments, ISqlContext context)
    {
        var state = new SqlPreprocessorState(context, segments.Count + 10);
        
        // ====================================================================
        // MICRO-OPTIMIZATION: Cache the extension rules flag outside the loop!
        // ====================================================================
        var rules = context.Options.PreprocessorRules;
        bool hasCustomRules = rules.Count > 0;

        for (int i = 0; i < segments.Count; i++)
        {
            var segment = segments[i];

            // 1. The Extension Pipeline (e.g., custom JSON operators, proprietary syntax)
            if (hasCustomRules)
            {
                bool handled = false;
                for (int r = 0; r < rules.Count; r++)
                {
                    if (rules[r].Process(ref segment, segments, i, state))
                    {
                        handled = true;
                        break;
                    }
                }
                
                // If a custom rule took ownership of this segment, skip the core pipeline
                if (handled) continue;
            }

            // 2. Core Structural State Machine (Tier 1)
            // Any non-literal (parameters, fragments, etc.) is simply passed through untouched
            // bypassing any deep expression parsing or allocation.
            if (segment.Type != SqlSegmentType.Literal)
            {
                state.Refined.Add(segment);
                continue;
            }
            
            // 3. Fast O(N) Forward-Scan Lexer for block keywords
            if (segment.Value is string textLiteral)
            {
                // We pass 'ref i' so the lexer can advance the outer segment loop 
                // if it consumes an interpolated parameter (e.g. for a LIMIT or OFFSET value)
                ProcessTextLiteral(segment, textLiteral, segments, ref i, state);
                continue;
            }

            state.Refined.Add(segment);
        }

        return state.Refined;
    }
}