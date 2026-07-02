namespace SqlInterpol.Parsing;

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
        var state = new PreprocessorState(context, segments.Count + 10);

        for (int i = 0; i < segments.Count; i++)
        {
            var segment = segments[i];

            if (ProcessSubquery(segment, i, segments, state)) continue;
            if (ProcessHoleBoundAlias(segment, state)) continue;
            
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