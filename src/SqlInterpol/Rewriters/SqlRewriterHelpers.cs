namespace SqlInterpol;

/// <summary>
/// High-performance shared helpers for AST segment rewriting and dialect transpilation passes.
/// </summary>
public static class SqlRewriterHelpers
{
    /// <summary>
    /// Safely extracts limit and offset parameter segments from a paging initiator token sequence.
    /// </summary>
    public static bool TryExtractPagingParameters(
        IReadOnlyList<SqlSegment> segments, 
        int currentIndex, 
        out SqlSegment limitParam, 
        out SqlSegment offsetParam, 
        out int nextIndex)
    {
        limitParam = default;
        offsetParam = default;
        nextIndex = currentIndex;

        int p1Idx = currentIndex + 1;
        while (p1Idx < segments.Count && segments[p1Idx].Type == SqlSegmentType.Literal && string.IsNullOrWhiteSpace(segments[p1Idx].Value as string)) p1Idx++;
        
        int p2Idx = p1Idx + 1;
        while (p2Idx < segments.Count && segments[p2Idx].Type == SqlSegmentType.Literal && string.IsNullOrWhiteSpace(segments[p2Idx].Value as string)) p2Idx++;
        
        int p3Idx = p2Idx + 1;
        while (p3Idx < segments.Count && segments[p3Idx].Type == SqlSegmentType.Literal && string.IsNullOrWhiteSpace(segments[p3Idx].Value as string)) p3Idx++;

        if (p3Idx < segments.Count && segments[p1Idx].Type == SqlSegmentType.Parameter && segments[p3Idx].Type == SqlSegmentType.Parameter)
        {
            limitParam = segments[p1Idx];
            offsetParam = segments[p3Idx];
            nextIndex = p3Idx;
            return true;
        }

        return false;
    }

    /// <summary>
    /// Evaluates whether a text segment safely contains an unquoted SQL clause phrase.
    /// </summary>
    public static bool ContainsKeyword(string? text, string keyword)
    {
        if (string.IsNullOrEmpty(text)) return false;

        int startIndex = 0;
        while ((startIndex = text.IndexOf(keyword, startIndex, StringComparison.OrdinalIgnoreCase)) >= 0)
        {
            bool leftOk = startIndex == 0 || (!char.IsLetterOrDigit(text[startIndex - 1]) && text[startIndex - 1] != '_');
            bool rightOk = startIndex + keyword.Length == text.Length || (!char.IsLetterOrDigit(text[startIndex + keyword.Length]) && text[startIndex + keyword.Length] != '_');
            
            if (leftOk && rightOk) return true;
            startIndex += keyword.Length;
        }
        return false;
    }
}