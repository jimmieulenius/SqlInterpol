using SqlInterpol.Configuration;
using SqlInterpol.Schema;
using SqlInterpol.Segments;

namespace SqlInterpol.Pipeline;

/// <summary>
/// The default semantic preprocessor that normalizes text, isolates core DML keywords, 
/// handles target entity aliases (both hole-bound and plain text), and routes projection mapping.
/// </summary>
public partial class SqlSegmentPreprocessor : ISqlSegmentPreprocessor
{
    public static readonly SqlSegmentPreprocessor Instance = new();

    /// <inheritdoc />
    public IReadOnlyList<SqlSegment> Process(IReadOnlyList<SqlSegment> segments, ISqlContext context)
    {
        var state = new SqlPreprocessorState(context, segments.Count + 10);
        
        // MICRO-OPTIMIZATION: Cache the extension rules flag outside the loop!
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

            if (ProcessSubquery(segment, i, segments, state)) continue;
            
            // 🌟 FIX: Check for hole-bound aliases BEFORE the Raw bypass, ensuring 
            // dynamic objects like {{p.Name}} injected after 'AS' are correctly captured!
            if (ProcessHoleBoundAlias(segment, state)) continue;

            // 2. The Core State Machine (Lightning Fast)
            if (segment.Type == SqlSegmentType.Raw)
            {
                // 🌟 FIX: Safely route dynamic OrderBy fragments to be resolved!
                if (segment.Value is SqlDynamicColumnFragment || 
                    segment.Value is SqlDynamicOrderFragment || 
                    segment.Value is SqlColumnReferenceBase || 
                    segment.Value is ISqlProjection)
                {
                    if (ProcessDmlContext(ref segment, segments, state)) continue;
                }

                state.Refined.Add(segment);
                state.ExpectsAlias = false;
                continue;
            }
            
            // Handles DML keywords, tracking, and structural rendering contexts (e.g. INSERT lists)
            if (ProcessDmlContext(ref segment, segments, state)) continue; 
            
            if (segment.Type == SqlSegmentType.Literal && segment.Value is string)
            {
                ProcessTextLiteral(segment, segments, i, state);
                continue;
            }
            
            if (ProcessUnresolved(segment, segments, state)) continue;

            state.Refined.Add(segment);
            state.ExpectsAlias = false;
        }

        return state.Refined;
    }
}